using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Shizou.Data.Database;
using Shizou.Data.Enums;
using Shizou.Data.Models;
using Shizou.Server.AniDbApi.Requests.Udp;
using Shizou.Server.AniDbApi.Requests.Udp.Interfaces;
using Shizou.Server.Extensions.Query;
using Shizou.Server.FileCaches;
using Shizou.Server.Options;
using Shizou.Server.Services;

namespace Shizou.Server.Commands.AniDb;

public sealed record ProcessArgs(int Id, IdTypeLocalFile IdType) : CommandArgs($"{nameof(ProcessCommand)}_id={Id}_type={IdType}");

[Command(CommandType.GetFile, CommandPriority.Normal, QueueType.AniDbUdp)]
public class ProcessCommand : Command<ProcessArgs>
{
    private readonly CommandService _commandService;
    private readonly ILogger<ProcessCommand> _logger;
    private readonly ShizouContext _context;
    private readonly AniDbFileResultCache _fileResultCache;
    private readonly IFileRequest _fileRequest;
    private readonly ShizouOptions _options;

    public ProcessCommand(
        ILogger<ProcessCommand> logger,
        ShizouContext context,
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

    protected override async Task ProcessInner()
    {
        var result = await GetFileResult();

        if (result is null)
            return;

        if (!_context.AniDbAnimes.Any(a => a.Id == result.AnimeId))
            _commandService.Dispatch(new AnimeArgs(result.AnimeId!.Value));

        UpdateDatabase(result);

        UpdateAniDb(result);

        Completed = true;
    }

    private void UpdateAniDb(FileResult result)
    {
        if ((FileIsGeneric(result) && _context.AniDbEpisodes.WithManualLinks().Any(e => e.Id == result.EpisodeId!.Value)) ||
            _context.LocalFiles.SingleOrDefault(lf => lf.Ed2k == result.Ed2K!) is not null)
            if (result.MyListId is null)
                _commandService.Dispatch(new UpdateMyListArgs(false, _options.AniDb.MyList.PresentFileState, Fid: result.FileId));
            else if (result.MyListState != _options.AniDb.MyList.PresentFileState || result.MyListFileState != MyListFileState.Normal)
                _commandService.Dispatch(new UpdateMyListArgs(true, _options.AniDb.MyList.PresentFileState, Lid: result.MyListId!));
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
            Audio = result.AudioCodecs!.Zip(result.AudioBitRates!, (codec, bitrate) => (codec, bitrate))
                .Zip(result.DubLanguages!, (tup, lang) => (tup.codec, tup.bitrate, lang)).Select((tuple, i) =>
                    new AniDbAudio { Bitrate = tuple.bitrate, Codec = tuple.codec, Language = tuple.lang, Id = i + 1, AniDbFileId = result.FileId })
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
            Subtitles = result.SubLangugages!.Select((s, i) => new AniDbSubtitle { Language = s, Id = i + 1, AniDbFileId = result.FileId }).ToList(),
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
                }
        };
    }

    private void UpdateDatabase(FileResult result)
    {
        if (FileIsGeneric(result)) // Generics also have .rl extension but don't know if it's exclusive
            UpdateGenericFile(result);
        else
            UpdateFile(result);
    }

    private void UpdateFileWatchedState(FileResult result)
    {
        var eState = _context.FileWatchedStates.Find(result.FileId);
        if (eState is null)
            _context.FileWatchedStates.Add(new FileWatchedState
            {
                Id = result.FileId,
                Ed2k = result.Ed2K!,
                Watched = result.MyListViewed ?? false,
                WatchedUpdated = null,
                MyListId = result.MyListId
            });
        else if (eState.WatchedUpdated is null)
            eState.Watched = result.MyListViewed ?? false;
    }

    private void UpdateEpisodeWatchedState(FileResult result)
    {
        var eState = _context.EpisodeWatchedStates.Find(result.EpisodeId!.Value);
        if (eState is null)
            _context.EpisodeWatchedStates.Add(new EpisodeWatchedState
            {
                Id = result.EpisodeId!.Value,
                Watched = result.MyListViewed ?? false,
                WatchedUpdated = null,
                MyListId = result.MyListId
            });
        else if (eState.WatchedUpdated is null)
            eState.Watched = result.MyListViewed ?? false;
    }

    private static bool FileIsGeneric(FileResult result)
    {
        return result.Ed2K is null && result.State == 0;
    }

    private void UpdateFile(FileResult result)
    {
        _logger.LogInformation("Updating AniDb file information for file id {FileId}", result.FileId);
        var eFile = _context.AniDbFiles
            .Include(f => f.AniDbGroup)
            .AsSingleQuery()
            .SingleOrDefault(f => f.Id == result.FileId);
        var file = FileResultToAniDbFile(result);

        if (eFile is null)
            _context.Entry(file).State = EntityState.Added;
        else
            _context.Entry(eFile).CurrentValues.SetValues(file);

        UpdateNavigations(file);

        UpdateOwnedNavigations(file, eFile);

        UpdateEpRelations(result);

        UpdateFileWatchedState(result);

        _context.SaveChanges();
    }

    private void UpdateNavigations(AniDbFile file)
    {
        if (file.AniDbGroup is not null)
        {
            if (_context.AniDbGroups.Find(file.AniDbGroupId) is { } eAniDbGroup)
                _context.Entry(eAniDbGroup).CurrentValues.SetValues(file.AniDbGroup);
            else
                _context.Entry(file.AniDbGroup).State = EntityState.Added;
        }

        if (_context.LocalFiles.FirstOrDefault(lf => lf.Ed2k == file.Ed2k) is { } eLocalFile)
            file.LocalFile = eLocalFile;
    }

    private void UpdateOwnedNavigations(AniDbFile file, AniDbFile? eFile)
    {
        if (eFile is null)
        {
            if (file.Video is not null)
                _context.Entry(file.Video).State = EntityState.Added;
            foreach (var a in file.Audio)
                _context.Entry(a).State = EntityState.Added;
            foreach (var s in file.Subtitles)
                _context.Entry(s).State = EntityState.Added;
        }
        else
        {
            if (eFile.Video is not null && file.Video is not null)
                _context.Entry(eFile.Video).CurrentValues.SetValues(file.Video);
            else
                eFile.Video = file.Video;

            foreach (var a in eFile.Audio.ExceptBy(file.Audio.Select(a => a.Id), a => a.Id))
                eFile.Audio.Remove(a);
            foreach (var s in eFile.Subtitles.ExceptBy(file.Subtitles.Select(s => s.Id), s => s.Id))
                eFile.Subtitles.Remove(s);

            foreach (var a in file.Audio)
                if (eFile.Audio.FirstOrDefault(x => x.Id == a.Id) is var ea && ea is null)
                    eFile.Audio.Add(a);
                else
                    _context.Entry(ea).CurrentValues.SetValues(a);
            foreach (var s in file.Subtitles)
                if (eFile.Subtitles.FirstOrDefault(x => x.Id == s.Id) is var es && es is null)
                    eFile.Subtitles.Add(s);
                else
                    _context.Entry(es).CurrentValues.SetValues(s);
        }

    }

    private void UpdateGenericFile(FileResult result)
    {
        _logger.LogInformation("Updating generic AniDb file information for file id {FileId}", result.FileId);
        var eGenericFile = _context.AniDbGenericFiles
            .SingleOrDefault(f => f.Id == result.FileId);
        var genericFile = new AniDbGenericFile
        {
            Id = result.FileId,
            AniDbEpisodeId = result.EpisodeId!.Value
        };


        if (eGenericFile is null)
            _context.Entry(genericFile).State = EntityState.Added;
        else
            _context.Entry(eGenericFile).CurrentValues.SetValues(genericFile);

        _context.SaveChanges();

        UpdateEpisodeWatchedState(result);
    }

    private void UpdateEpRelations(FileResult result)
    {
        var eRels = _context.AniDbEpisodeFileXrefs.Where(x => x.AniDbFileId == result.FileId).ToList();
        var rels = (result.OtherEpisodeIds ?? new List<int>()).Append(result.EpisodeId!.Value)
            .Select(x => new AniDbEpisodeFileXref { AniDbEpisodeId = x, AniDbFileId = result.FileId }).ToList();

        _context.AniDbEpisodeFileXrefs.RemoveRange(eRels.ExceptBy(rels.Select(x => x.AniDbEpisodeId), x => x.AniDbEpisodeId));
        _context.AniDbEpisodeFileXrefs.AddRange(rels.ExceptBy(eRels.Select(x => x.AniDbEpisodeId), x => x.AniDbEpisodeId));

    }

    private async Task<FileResult?> GetFileResult()
    {
        string? fileResultCacheKey;
        FileResult? result;
        switch (CommandArgs.IdType)
        {
            case IdTypeLocalFile.LocalId:
            {
                // ReSharper disable once MethodHasAsyncOverload
                var localFile = _context.LocalFiles.Find(CommandArgs.Id);
                if (localFile is null)
                {
                    Completed = true;
                    _logger.LogWarning("Unable to process local file id: {LocalFileId} not found, skipping", CommandArgs.Id);
                    return null;
                }

                fileResultCacheKey = $"File_Ed2k={localFile.Ed2k}.json";
                result = await _fileResultCache.Get(fileResultCacheKey);
                if (result is not null)
                    return result;
                _logger.LogInformation("Processing local file id: {LocalfileId}, ed2k: {LocalFileEd2k}", CommandArgs.Id, localFile.Ed2k);
                _fileRequest.SetParameters(localFile.FileSize, localFile.Ed2k, FileRequest.DefaultFMask, FileRequest.DefaultAMask);
                break;
            }
            case IdTypeLocalFile.FileId:
                fileResultCacheKey = $"File_Id={CommandArgs.Id}.json";
                result = await _fileResultCache.Get(fileResultCacheKey);
                if (result is not null)
                    return result;
                _logger.LogInformation("Processing file id: {FileId}", CommandArgs.Id);
                _fileRequest.SetParameters(CommandArgs.Id, FileRequest.DefaultFMask, FileRequest.DefaultAMask);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(CommandArgs.IdType), CommandArgs.IdType, null);
        }

        await _fileRequest.Process();
        result = _fileRequest.FileResult;
        if (_fileRequest.ResponseCode == AniDbResponseCode.NoSuchFile)
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
            await _fileResultCache.Save(fileResultCacheKey, result);
        }

        return result;
    }
}
