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
using Shizou.Server.Extensions;
using Shizou.Server.FileCaches;
using Shizou.Server.Options;
using Shizou.Server.Services;

namespace Shizou.Server.Commands.AniDb;

public sealed record ProcessArgs(int Id, IdTypeLocalFile IdType) : CommandArgs($"{nameof(ProcessCommand)}_id={Id}_type={IdType}");

[Command(CommandType.GetFile, CommandPriority.Normal, QueueType.AniDbUdp)]
public class ProcessCommand : BaseCommand<ProcessArgs>
{
    private readonly CommandService _commandService;
    private readonly ILogger<ProcessCommand> _logger;
    private readonly ShizouContext _context;
    private readonly AniDbFileResultCache _fileResultCache;
    private readonly UdpRequestFactory _udpRequestFactory;
    private readonly ShizouOptions _options;

    public ProcessCommand(
        ILogger<ProcessCommand> logger,
        ShizouContext context,
        CommandService commandService,
        AniDbFileResultCache fileResultCache,
        UdpRequestFactory udpRequestFactory,
        IOptionsSnapshot<ShizouOptions> optionsSnapshot)
    {
        _logger = logger;
        _context = context;
        _commandService = commandService;
        _fileResultCache = fileResultCache;
        _udpRequestFactory = udpRequestFactory;
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
        if ((FileIsGeneric(result) && _context.EpisodesWithManualLinks.Any(e => e.Id == result.EpisodeId!.Value)) ||
            _context.LocalFiles.GetByEd2K(result.Ed2K!) is not null)
            if (result.MyListId is null)
                _commandService.Dispatch(new UpdateMyListArgs(false, _options.MyList.PresentFileState, Fid: result.FileId));
            else if (result.MyListState != _options.MyList.PresentFileState || result.MyListFileState != MyListFileState.Normal)
                _commandService.Dispatch(new UpdateMyListArgs(true, _options.MyList.PresentFileState, Lid: result.MyListId!));
    }

    private static AniDbMyListEntry? FileResultToAniDbMyListEntry(FileResult result)
    {
        return result.MyListId is null
            ? null
            : new AniDbMyListEntry
            {
                Id = result.MyListId.Value,
                FileId = result.FileId,
                Watched = result.MyListViewed!.Value,
                WatchedDate = result.MyListViewDate?.UtcDateTime,
                MyListState = result.MyListState!.Value,
                MyListFileState = result.MyListFileState!.Value,
                Updated = DateTime.UtcNow
            };
    }

    private static AniDbFile FileResultToAniDbFile(FileResult result)
    {
        return new AniDbFile
        {
            Id = result.FileId,
            Ed2K = result.Ed2K!,
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
            Watched = result.MyListViewed ?? false,
            WatchedUpdatedLocally = null,
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

        UpdateMyListEntry(result);
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

        UpdateNavigations(file);

        if (eFile is null)
            _context.Entry(file).State = EntityState.Added;
        else
        {
            // Only update local watched state if it wasn't set by user, we can wait for a mylist sync
            if (eFile.WatchedUpdatedLocally is not null)
            {
                file.Watched = eFile.Watched;
                file.WatchedUpdatedLocally = eFile.WatchedUpdatedLocally;
            }
            _context.Entry(eFile).CurrentValues.SetValues(file);
        }
        _context.SaveChanges();

        UpdateOwnedNavigations(file, eFile);

        UpdateEpRelations(result);
    }

    private void UpdateNavigations(AniDbFile file)
    {
        if (file.AniDbGroup is not null)
        {
            var eAniDbGroup = _context.AniDbGroups.Find(file.AniDbGroupId);
            if (eAniDbGroup is null)
                _context.AniDbGroups.Add(file.AniDbGroup);
            else
                _context.Entry(eAniDbGroup).CurrentValues.SetValues(file.AniDbGroup);
        }
        _context.SaveChanges();
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
        _context.SaveChanges();
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

        var eEpisode = _context.AniDbEpisodes.Find(result.EpisodeId!.Value);
        // Only update local watched state if it wasn't set by user, we can wait for a mylist sync
        if (eEpisode is not null && eEpisode.WatchedUpdatedLocally is null)
        {
            eEpisode.Watched = result.MyListViewed ?? false;
            eEpisode.WatchedUpdatedLocally = null;
        }
        _context.SaveChanges();
    }

    private void UpdateMyListEntry(FileResult result)
    {
        var entry = FileResultToAniDbMyListEntry(result);
        if (entry is not null)
        {
            var eEntry = _context.AniDbMyListEntries.Find(entry.Id);
            if (eEntry is null)
                _context.AniDbMyListEntries.Add(entry);
            else
                _context.Entry(eEntry).CurrentValues.SetValues(entry);
            _context.SaveChanges();
        }
    }

    private void UpdateEpRelations(FileResult result)
    {
        var eRels = _context.AniDbEpisodeFileXrefs.Where(x => x.AniDbFileId == result.FileId).ToList();
        var rels = (result.OtherEpisodeIds ?? new List<int>()).Append(result.EpisodeId!.Value)
            .Select(x => new AniDbEpisodeFileXref { AniDbEpisodeId = x, AniDbFileId = result.FileId }).ToList();

        _context.AniDbEpisodeFileXrefs.RemoveRange(eRels.ExceptBy(rels.Select(x => x.AniDbEpisodeId), x => x.AniDbEpisodeId));
        _context.AniDbEpisodeFileXrefs.AddRange(rels.ExceptBy(eRels.Select(x => x.AniDbEpisodeId), x => x.AniDbEpisodeId));

        _context.SaveChanges();
    }

    private async Task<FileResult?> GetFileResult()
    {
        string? fileResultCacheKey;
        FileRequest? fileReq;
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
                fileResultCacheKey = $"File_Ed2k={localFile.Ed2K}.json";
                result = await _fileResultCache.Get(fileResultCacheKey);
                if (result is not null)
                    return result;
                _logger.LogInformation("Processing local file id: {LocalfileId}, ed2k: {LocalFileEd2k}", CommandArgs.Id, localFile.Ed2K);
                fileReq = _udpRequestFactory.FileRequest(localFile.FileSize, localFile.Ed2K, FileRequest.DefaultFMask, FileRequest.DefaultAMask);
                break;
            }
            case IdTypeLocalFile.FileId:
                fileResultCacheKey = $"File_Id={CommandArgs.Id}.json";
                result = await _fileResultCache.Get(fileResultCacheKey);
                if (result is not null)
                    return result;
                _logger.LogInformation("Processing file id: {FileId}", CommandArgs.Id);
                fileReq = _udpRequestFactory.FileRequest(CommandArgs.Id, FileRequest.DefaultFMask, FileRequest.DefaultAMask);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(CommandArgs.IdType), CommandArgs.IdType, null);
        }

        await fileReq.Process();
        result = fileReq.FileResult;
        if (fileReq.ResponseCode == AniDbResponseCode.NoSuchFile)
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
