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

    private static AniDbNormalFile FileResultToAniDbFile(FileResult result)
    {
        return new AniDbNormalFile
        {
            Id = result.FileId,
            Ed2k = result.Ed2K ?? throw new NullReferenceException("Ed2k is null, cannot be null for non-generic file"),
            Md5 = result.Md5,
            Crc = result.Crc32,
            Sha1 = result.Sha1,
            Censored = result.State.IsCensored(),
            Chaptered = result.State.HasFlag(FileState.Chaptered),
            Deprecated = result.IsDeprecated,
            FileSize = result.Size,
            DurationSeconds = result.LengthInSeconds,
            Source = result.Source,
            FileVersion = result.State.FileVersion(),
            Updated = DateTime.UtcNow,
            Audio = result.AudioCodecs.Zip(result.AudioBitRates,
                    (codec, bitrate) => (codec, bitrate))
                .Zip(result.DubLanguages,
                    (tup,
                        lang) => (tup.codec, tup.bitrate, lang))
                .Select(tuple => new AniDbAudio
                {
                    Bitrate = tuple.bitrate,
                    Codec = tuple.codec,
                    Language = tuple.lang
                })
                .ToList(),
            Video = result.VideoCodec is null
                ? null
                : new AniDbVideo
                {
                    Codec = result.VideoCodec,
                    BitRate = result.VideoBitRate ?? throw new NullReferenceException("Video bitrate null when codec is not"),
                    ColorDepth = result.VideoColorDepth ?? 8,
                    Height = result.VideoResolution is not null
                        ? int.Parse(result.VideoResolution.Split('x')[1])
                        : throw new NullReferenceException("Video Resolution null when codec is not"),
                    Width = int.Parse(result.VideoResolution.Split('x')[0])
                },
            Subtitles = result.SubLangugages.Select(s => new AniDbSubtitle
                {
                    Language = s
                })
                .ToList(),
            FileName = result.AniDbFileName,
            AniDbGroupId = result.GroupId,
            AniDbGroup = result.GroupId is null
                ? null
                : new AniDbGroup
                {
                    Id = result.GroupId.Value,
                    Name = result.GroupName ?? throw new NullReferenceException("Group name null when group id not"),
                    ShortName = result.GroupNameShort ?? throw new NullReferenceException("Group short name null when group id not"),
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

    private static bool FileIsGeneric(FileResult result) => result.Ed2K is null && result.State == 0;

    protected override async Task ProcessInnerAsync()
    {
        var result = await GetFileResultAsync().ConfigureAwait(false);

        if (result is null)
            return;

        UpdateDatabase(result);

        UpdateAniDb(result);

        // Dispatch after updating db to minimize conflict with xref creation
        if (!_context.AniDbAnimes.Any(a => a.Id == result.AnimeId) || !_context.AniDbEpisodes.Any(ep => ep.Id == result.EpisodeId))
            _commandService.Dispatch(new AnimeArgs(result.AnimeId));

        Completed = true;
    }

    private void UpdateAniDb(FileResult result)
    {
        var aniDbFile = _context.AniDbFiles.AsNoTracking().Include(f => f.FileWatchedState).Include(f => f.LocalFiles).Single(f => f.Id == result.FileId);
        var presentState = _options.AniDb.MyList.PresentFileState;
        var watchedState = aniDbFile.FileWatchedState;
        if (aniDbFile.LocalFiles.Any())
        {
            if (watchedState.MyListId is null)
                _commandService.Dispatch(new AddMyListArgs(aniDbFile.Id, presentState, watchedState.Watched,
                    watchedState.WatchedUpdated));
            else if (watchedState.Watched != result.MyListViewed)
                _commandService.Dispatch(new UpdateMyListArgs(watchedState.MyListId.Value, presentState,
                    watchedState.Watched, watchedState.WatchedUpdated));
            else if (result.MyListState != presentState || result.MyListFileState != MyListFileState.Normal)
                _commandService.Dispatch(new UpdateMyListArgs(watchedState.MyListId.Value, presentState, null, null));
        }
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
        var eFile = _context.AniDbNormalFiles.FirstOrDefault(f => f.Id == result.FileId);
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
            eFile.Video = file.Video;
            eFile.Audio = file.Audio;
            eFile.Subtitles = file.Subtitles;
        }

        UpdateNavigations(file);

        UpdateEpRelations(result.FileId, new List<int> { result.EpisodeId }.Concat(result.OtherEpisodeIds).ToList());

        _context.SaveChanges();
    }

    private void UpdateNavigations(AniDbNormalFile file)
    {
        if (file.AniDbGroup is not null)
            if (_context.AniDbGroups.FirstOrDefault(g => g.Id == file.AniDbGroupId) is { } eAniDbGroup)
                _context.Entry(eAniDbGroup).CurrentValues.SetValues(file.AniDbGroup);
            else
                _context.Entry(file.AniDbGroup).State = EntityState.Added;

        if (_context.FileWatchedStates.FirstOrDefault(ws => ws.AniDbFileId == file.Id) is var eFileWatchedState && eFileWatchedState is not null)
            _context.Entry(eFileWatchedState).CurrentValues.SetValues(file.FileWatchedState);
        else
            _context.Entry(file.FileWatchedState).State = EntityState.Added;

        if (_context.LocalFiles.Include(lf => lf.AniDbFile)
                .ThenInclude(f => f!.FileWatchedState)
                .Include(lf => lf.AniDbFile)
                .ThenInclude(f => f!.AniDbEpisodeFileXrefs)
                .FirstOrDefault(lf => lf.Ed2k == file.Ed2k) is { } eLocalFile)
        {
            if (eLocalFile.AniDbFile is AniDbGenericFile)
            {
                _logger.LogInformation("Replacing manual link from local file {LocalId} with episode {EpisodeId} with file relation", eLocalFile.Id,
                    eLocalFile.AniDbFile.AniDbEpisodeFileXrefs.First().AniDbEpisodeId);
                var ws = eFileWatchedState ?? file.FileWatchedState;
                ws.Watched = eLocalFile.AniDbFile.FileWatchedState.Watched;
                ws.WatchedUpdated = eLocalFile.AniDbFile.FileWatchedState.WatchedUpdated;
            }

            eLocalFile.AniDbFileId = file.Id;
        }
    }

    private void UpdateGenericFile(FileResult result)
    {
        _logger.LogInformation("Updating generic AniDb file information for file id {FileId}", result.FileId);

        if (_context.FileWatchedStates.FirstOrDefault(ws => ws.AniDbFileId == result.FileId) is { } eWs)
        {
            eWs.Watched = result.MyListViewed ?? false;
            eWs.MyListId = result.MyListId;
            eWs.WatchedUpdated = null;
            _context.SaveChanges();
        }
        else if (_context.AniDbEpisodes.Include(ep => ep.AniDbFiles).FirstOrDefault(ep => ep.Id == result.EpisodeId) is { } eEp)
        {
            if (eEp.AniDbFiles.OfType<AniDbGenericFile>().FirstOrDefault() is { } eFile)
            {
                if (eFile.Id != result.FileId)
                    throw new InvalidOperationException(
                        $"Exising generic file for episode id {eFile.Id} does not match returned generic file id {result.FileId}");
            }
            else
            {
                eEp.AniDbFiles.Add(new AniDbGenericFile
                {
                    Id = result.FileId,
                    FileWatchedState = new FileWatchedState
                    {
                        Watched = result.MyListViewed ?? false,
                        WatchedUpdated = null,
                        MyListId = result.MyListId,
                        AniDbFileId = result.FileId
                    }
                });
                _context.SaveChanges();
            }
        }
        else
        {
            _logger.LogWarning("Failed to update generic file id {FileId}, did not find episode id {EpisodeId}", result.FileId, result.EpisodeId);
        }
    }

    private void UpdateEpRelations(int fileId, List<int> episodeIds)
    {
        var eRels = _context.AniDbEpisodeFileXrefs.Where(x => x.AniDbFileId == fileId).ToList();
        var eHangingRels = _context.HangingEpisodeFileXrefs.Where(x => x.AniDbNormalFileId == fileId).ToList();
        _context.AniDbEpisodeFileXrefs.RemoveRange(eRels.ExceptBy(episodeIds, x => x.AniDbEpisodeId));
        _context.HangingEpisodeFileXrefs.RemoveRange(eHangingRels.ExceptBy(episodeIds, x => x.AniDbEpisodeId));
        foreach (var rel in episodeIds
                     .Except(eRels.Select(x => x.AniDbEpisodeId))
                     .Except(eHangingRels.Select(x => x.AniDbEpisodeId)))
            if (_context.AniDbEpisodes.Any(ep => ep.Id == rel))
                _context.AniDbEpisodeFileXrefs.Add(new AniDbEpisodeFileXref
                {
                    AniDbEpisodeId = rel,
                    AniDbFileId = fileId
                });
            else
                _context.HangingEpisodeFileXrefs.Add(new HangingEpisodeFileXref
                {
                    AniDbEpisodeId = rel,
                    AniDbNormalFileId = fileId
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
                _fileRequest.SetParameters(localFile.FileSize, localFile.Ed2k);
                break;
            }
            case IdTypeLocalOrFile.FileId:
                fileCacheFilename = $"File_Id={CommandArgs.Id}.json";
                result = await _fileResultCache.GetAsync(fileCacheFilename).ConfigureAwait(false);
                if (result is not null)
                    return result;
                _logger.LogInformation("Processing file id: {FileId}", CommandArgs.Id);
                _fileRequest.SetParameters(CommandArgs.Id);
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
