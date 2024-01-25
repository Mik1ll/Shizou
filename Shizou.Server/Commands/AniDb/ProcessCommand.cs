using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Shizou.Data.CommandInputArgs;
using Shizou.Data.Database;
using Shizou.Data.Enums;
using Shizou.Data.Models;
using Shizou.Server.AniDbApi.Requests.Udp;
using Shizou.Server.AniDbApi.Requests.Udp.Interfaces;
using Shizou.Server.FileCaches;
using Shizou.Server.Options;
using Shizou.Server.Services;

namespace Shizou.Server.Commands.AniDb;

public class ProcessCommand : Command<ProcessArgs>
{
    private readonly CommandService _commandService;
    private readonly ILogger<ProcessCommand> _logger;
    private readonly IShizouContext _context;
    private readonly AniDbFileResultCache _fileResultCache;
    private readonly IFileRequest _fileRequest;
    private readonly ShizouOptions _options;

    public ProcessCommand(
        ILogger<ProcessCommand> logger,
        IShizouContext context,
        CommandService commandService,
        AniDbFileResultCache fileResultCache,
        IOptionsSnapshot<ShizouOptions> optionsSnapshot,
        IFileRequest fileRequest)
    {
        _logger = logger;
        _context = context;
        _commandService = commandService;
        _fileResultCache = fileResultCache;
        _fileRequest = fileRequest;
        _options = optionsSnapshot.Value;
    }

    private static AniDbFile FileResultToAniDbFile(FileResult result)
    {
        return new AniDbFile
        {
            Id = result.FileId,
            Ed2k = result.Ed2K!,
            Md5 = result.Md5,
            Crc = result.Crc32,
            Sha1 = result.Sha1,
            Censored = result.State!.Value.IsCensored(),
            Chaptered = result.State!.Value.HasFlag(FileState.Chaptered),
            Deprecated = result.IsDeprecated!.Value,
            FileSize = result.Size!.Value,
            DurationSeconds = result.LengthInSeconds,
            Source = result.Source,
            FileVersion = result.State!.Value.FileVersion(),
            Updated = DateTime.UtcNow,
            Audio = result.AudioCodecs!.Zip(result.AudioBitRates!,
                    (codec, bitrate) => (codec, bitrate))
                .Zip(result.DubLanguages!,
                    (tup,
                        lang) => (tup.codec, tup.bitrate, lang))
                .Select((tuple,
                        i) =>
                    new AniDbAudio
                    {
                        Bitrate = tuple.bitrate,
                        Codec = tuple.codec,
                        Language = tuple.lang,
                        Id = i + 1,
                        AniDbFileId = result.FileId
                    })
                .ToList(),
            Video = result.VideoCodec is null
                ? null
                : new AniDbVideo
                {
                    Codec = result.VideoCodec,
                    BitRate = result.VideoBitRate!.Value,
                    ColorDepth = result.VideoColorDepth ?? 8,
                    Height = int.Parse(result.VideoResolution!.Split('x')[1]),
                    Width = int.Parse(result.VideoResolution!.Split('x')[0])
                },
            Subtitles = result.SubLangugages!.Select((s,
                    i) => new AniDbSubtitle
                {
                    Language = s,
                    Id = i + 1,
                    AniDbFileId = result.FileId
                })
                .ToList(),
            FileName = result.AniDbFileName!,
            AniDbGroupId = result.GroupId,
            AniDbGroup = result.GroupId is null
                ? null
                : new AniDbGroup
                {
                    Id = result.GroupId!.Value,
                    Name = result.GroupName!,
                    ShortName = result.GroupNameShort!,
                    Url = null
                },
            FileWatchedState = new FileWatchedState
            {
                AniDbFileId = result.FileId,
                Watched = result.MyListViewed ?? false,
                WatchedUpdated = null,
                MyListId = result.MyListId
            }
        };
    }

    private static bool FileIsGeneric(FileResult result)
    {
        return result.Ed2K is null && result.State == 0;
    }

    protected override async Task ProcessInnerAsync()
    {
        var result = await GetFileResultAsync().ConfigureAwait(false);

        if (result is null)
            return;

        UpdateDatabase(result);

        UpdateAniDb(result);

        // Dispatch after updating db to minimize conflict with xref creation
        if (!_context.AniDbAnimes.Any(a => a.Id == result.AnimeId))
            _commandService.Dispatch(new AnimeArgs(result.AnimeId!.Value));

        Completed = true;
    }

    private void UpdateAniDb(FileResult result)
    {
        if ((FileIsGeneric(result) && _context.AniDbEpisodes.Any(ep => ep.Id == result.EpisodeId!.Value && ep.ManualLinkLocalFiles.Any())) ||
            _context.LocalFiles.SingleOrDefault(lf => lf.Ed2k == result.Ed2K!) is not null)
            if (result.MyListId is null)
                _commandService.Dispatch(new UpdateMyListArgs(false, _options.AniDb.MyList.PresentFileState, Fid: result.FileId));
            else if (result.MyListState != _options.AniDb.MyList.PresentFileState || result.MyListFileState != MyListFileState.Normal)
                _commandService.Dispatch(new UpdateMyListArgs(true, _options.AniDb.MyList.PresentFileState, Lid: result.MyListId!));
    }

    private void UpdateDatabase(FileResult result)
    {
        if (FileIsGeneric(result)) // Generics also have .rl extension but don't know if it's exclusive
            UpdateGenericFile(result);
        else
            UpdateFile(result);
    }

    private void UpdateFile(FileResult result)
    {
        _logger.LogInformation("Updating AniDb file information for file id {FileId}", result.FileId);
        var eFile = _context.AniDbFiles.AsSplitQuery().FirstOrDefault(f => f.Id == result.FileId);
        var file = FileResultToAniDbFile(result);

        if (eFile is null)
        {
            _context.Entry(file).State = EntityState.Added;
            if (file.Video is not null)
                _context.Entry(file.Video).State = EntityState.Added;
            foreach (var a in file.Audio)
                _context.Entry(a).State = EntityState.Added;
            foreach (var s in file.Subtitles)
                _context.Entry(s).State = EntityState.Added;
        }
        else
        {
            _context.Entry(eFile).CurrentValues.SetValues(file);
            UpdateOwnedNavigations(file, eFile);
        }

        UpdateNavigations(file);

        UpdateEpRelations(result);

        _context.SaveChanges();
    }

    private void UpdateNavigations(AniDbFile file)
    {
        if (file.AniDbGroup is not null)
            if (_context.AniDbGroups.FirstOrDefault(g => g.Id == file.AniDbGroupId) is { } eAniDbGroup)
                _context.Entry(eAniDbGroup).CurrentValues.SetValues(file.AniDbGroup);
            else
                _context.Entry(file.AniDbGroup).State = EntityState.Added;

        if (_context.FileWatchedStates.FirstOrDefault(ws => ws.AniDbFileId == file.Id) is { } fileWatchedState)
        {
            if (fileWatchedState.WatchedUpdated is null)
                fileWatchedState.Watched = file.FileWatchedState.Watched;
            fileWatchedState.MyListId = file.FileWatchedState.MyListId;
        }
        else
        {
            _context.Entry(file.FileWatchedState).State = EntityState.Added;
            fileWatchedState = file.FileWatchedState;
        }

        if (_context.LocalFiles.FirstOrDefault(lf => lf.Ed2k == file.Ed2k) is { } eLocalFile)
        {
            eLocalFile.AniDbFileId = file.Id;
            if (eLocalFile.ManualLinkEpisodeId is not null)
            {
                _logger.LogInformation("Replacing manual link from local file {LocalId} with episode {EpisodeId} with file relation", eLocalFile.Id,
                    eLocalFile.ManualLinkEpisodeId);
                eLocalFile.ManualLinkEpisodeId = null;
                if (_context.EpisodeWatchedStates.FirstOrDefault(ws => ws.AniDbEpisodeId == eLocalFile.ManualLinkEpisodeId) is { } eEpisodeWatchedState)
                {
                    fileWatchedState.Watched = eEpisodeWatchedState.Watched;
                    fileWatchedState.WatchedUpdated = eEpisodeWatchedState.WatchedUpdated;
                }
            }
        }
    }

    private void UpdateOwnedNavigations(AniDbFile file, AniDbFile eFile)
    {
        if (eFile.Video is not null && file.Video is not null)
            _context.Entry(eFile.Video).CurrentValues.SetValues(file.Video);
        else
            eFile.Video = file.Video;

        foreach (var a in eFile.Audio.ExceptBy(file.Audio.Select(a => a.Id), a => a.Id))
            eFile.Audio.Remove(a);
        foreach (var a in file.Audio)
            if (eFile.Audio.FirstOrDefault(x => x.Id == a.Id) is { } ea)
                _context.Entry(ea).CurrentValues.SetValues(a);
            else
                eFile.Audio.Add(a);

        foreach (var s in eFile.Subtitles.ExceptBy(file.Subtitles.Select(s => s.Id), s => s.Id))
            eFile.Subtitles.Remove(s);
        foreach (var s in file.Subtitles)
            if (eFile.Subtitles.FirstOrDefault(x => x.Id == s.Id) is { } es)
                _context.Entry(es).CurrentValues.SetValues(s);
            else
                eFile.Subtitles.Add(s);
    }

    private void UpdateGenericFile(FileResult result)
    {
        _logger.LogInformation("Updating generic AniDb file information for file id {FileId}", result.FileId);

        if (_context.EpisodeWatchedStates.FirstOrDefault(ws => ws.AniDbEpisodeId == result.EpisodeId!.Value) is { } eEpisodeWatchedState)
        {
            if (eEpisodeWatchedState.WatchedUpdated is null)
                eEpisodeWatchedState.Watched = result.MyListViewed ?? false;
            eEpisodeWatchedState.AniDbFileId = result.FileId;
            eEpisodeWatchedState.MyListId = result.MyListId;
            _context.SaveChanges();
        }
        else
        {
            _logger.LogWarning("Failed to update generic file id {FileId}, did not find episode {EpisodeId} watch state", result.FileId, result.EpisodeId);
        }
    }

    private void UpdateEpRelations(FileResult result)
    {
        var relIds = new List<int> { result.EpisodeId!.Value }.Concat(result.OtherEpisodeIds ?? new List<int>()).ToList();
        var eRels = _context.AniDbEpisodeFileXrefs.Where(x => x.AniDbFileId == result.FileId).ToList();
        var eHangingRels = _context.HangingEpisodeFileXrefs.Where(x => x.AniDbFileId == result.FileId).ToList();
        _context.AniDbEpisodeFileXrefs.RemoveRange(eRels.ExceptBy(relIds, x => x.AniDbEpisodeId));
        _context.HangingEpisodeFileXrefs.RemoveRange(eHangingRels.ExceptBy(relIds, x => x.AniDbEpisodeId));
        foreach (var rel in relIds
                     .Except(eRels.Select(x => x.AniDbEpisodeId))
                     .Except(eHangingRels.Select(x => x.AniDbEpisodeId)))
            if (_context.AniDbEpisodes.Any(ep => ep.Id == rel))
                _context.AniDbEpisodeFileXrefs.Add(new AniDbEpisodeFileXref
                {
                    AniDbEpisodeId = rel,
                    AniDbFileId = result.FileId
                });
            else
                _context.HangingEpisodeFileXrefs.Add(new HangingEpisodeFileXref
                {
                    AniDbEpisodeId = rel,
                    AniDbFileId = result.FileId
                });
    }

    private async Task<FileResult?> GetFileResultAsync()
    {
        string? fileCacheFilename;
        FileResult? result;
        switch (CommandArgs.IdType)
        {
            case IdTypeLocalOrFile.LocalId:
            {
                var localFile = _context.LocalFiles.Find(CommandArgs.Id);
                if (localFile is null)
                {
                    Completed = true;
                    _logger.LogWarning("Unable to process local file id: {LocalFileId} not found, skipping", CommandArgs.Id);
                    return null;
                }

                fileCacheFilename = $"File_Ed2k={localFile.Ed2k}.json";
                result = await _fileResultCache.GetAsync(fileCacheFilename).ConfigureAwait(false);
                if (result is not null)
                    return result;
                _logger.LogInformation("Processing local file id: {LocalfileId}, ed2k: {LocalFileEd2k}", CommandArgs.Id, localFile.Ed2k);
                _fileRequest.SetParameters(localFile.FileSize, localFile.Ed2k, FileRequest.DefaultFMask, FileRequest.DefaultAMask);
                break;
            }
            case IdTypeLocalOrFile.FileId:
                fileCacheFilename = $"File_Id={CommandArgs.Id}.json";
                result = await _fileResultCache.GetAsync(fileCacheFilename).ConfigureAwait(false);
                if (result is not null)
                    return result;
                _logger.LogInformation("Processing file id: {FileId}", CommandArgs.Id);
                _fileRequest.SetParameters(CommandArgs.Id, FileRequest.DefaultFMask, FileRequest.DefaultAMask);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(CommandArgs.IdType), CommandArgs.IdType, null);
        }

        var response = await _fileRequest.ProcessAsync().ConfigureAwait(false);
        result = response?.FileResult;
        if (response?.ResponseCode == AniDbResponseCode.NoSuchFile)
        {
            _logger.LogInformation("Skipped processing {IdName}: {Id}, file not found on anidb", Enum.GetName(CommandArgs.IdType), CommandArgs.Id);
            Completed = true;
        }
        else if (result is null)
        {
            _logger.LogError("Could not process  {IdName}: {Id}, no file result", Enum.GetName(CommandArgs.IdType), CommandArgs.Id);
        }
        else
        {
            await _fileResultCache.SaveAsync(fileCacheFilename, result).ConfigureAwait(false);
        }

        return result;
    }
}
