using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Shizou.Data.Database;
using Shizou.Data.Enums;
using Shizou.Data.Models;
using Shizou.Server.AniDbApi.Requests.Udp;
using Shizou.Server.AniDbApi.Requests.Udp.Results;
using Shizou.Server.FileCaches;
using Shizou.Server.Services;

namespace Shizou.Server.Commands.AniDb;

public enum IdType
{
    LocalId = 1,
    FileId = 2
}

public sealed record ProcessArgs(int Id, IdType IdType) : CommandArgs($"{nameof(ProcessCommand)}_id={Id}_type={IdType}");

[Command(CommandType.GetFile, CommandPriority.Normal, QueueType.AniDbUdp)]
public class ProcessCommand : BaseCommand<ProcessArgs>
{
    private readonly CommandService _commandService;
    private readonly ILogger<ProcessCommand> _logger;
    private readonly ShizouContext _context;
    private readonly AniDbFileResultCache _fileResultCache;
    private readonly UdpRequestFactory _udpRequestFactory;
    private string _fileResultCacheKey = null!;

    public ProcessCommand(
        ILogger<ProcessCommand> logger,
        ShizouContext context,
        CommandService commandService,
        AniDbFileResultCache fileResultCache,
        UdpRequestFactory udpRequestFactory
    )
    {
        _logger = logger;
        _context = context;
        _commandService = commandService;
        _fileResultCache = fileResultCache;
        _udpRequestFactory = udpRequestFactory;
    }

    public override void SetParameters(CommandArgs args)
    {
        var pargs = (ProcessArgs)args;
        _fileResultCacheKey = $"File_{pargs.IdType.ToString()}_{pargs.Id}.json";
        base.SetParameters(args);
    }

    protected override async Task ProcessInner()
    {
        var result = await GetFileResult();

        if (result is null)
            return;

        UpdateDatabase(result);

        _commandService.Dispatch(new AnimeArgs(result.AnimeId!.Value));

        Completed = true;
    }

    private static AniDbMyListEntry FileResultToAniDbMyListEntry(AniDbFileResult result)
    {
        return new AniDbMyListEntry
        {
            Id = result.MyListId!.Value,
            Watched = result.MyListViewed!.Value,
            WatchedDate = result.MyListViewDate?.UtcDateTime,
            MyListState = result.MyListState!.Value,
            MyListFileState = result.MyListFileState!.Value
        };
    }

    private static AniDbFile FileResultToAniDbFile(AniDbFileResult result)
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
            MyListEntryId = result.MyListId,
            MyListEntry = result.MyListId is null
                ? null
                : FileResultToAniDbMyListEntry(result),
            Audio = result.AudioCodecs!.Zip(result.AudioBitRates!, (codec, bitrate) => (codec, bitrate))
                .Zip(result.DubLanguages!, (tup, lang) => (tup.codec, tup.bitrate, lang)).Select((tuple, i) =>
                    new AniDbAudio { Bitrate = tuple.bitrate, Codec = tuple.codec, Language = tuple.lang, Id = i + 1, AniDbFileId = result.FileId }).ToList(),
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

    private void UpdateDatabase(AniDbFileResult result)
    {
        if (result.Ed2K is null && result.State == 0) // Generics also have .rl extension but don't know if it's exclusive
            UpdateGenericFile(result);
        else
            UpdateFile(result);
    }

    private void UpdateFile(AniDbFileResult result)
    {
        var eFile = _context.AniDbFiles
            .Include(f => f.AniDbGroup)
            .Include(f => f.MyListEntry)
            .AsSingleQuery()
            .SingleOrDefault(f => f.Id == result.FileId);
        var file = FileResultToAniDbFile(result);

        UpdateNavigations(file);

        if (eFile is null)
            _context.Entry(file).State = EntityState.Added;
        else
            _context.Entry(eFile).CurrentValues.SetValues(file);
        _context.SaveChanges();

        UpdateOwnedNavigations(file, eFile);

        UpdateEpRelations(result);
    }

    private void UpdateNavigations(AniDbFile file)
    {
        UpdateMyListEntry(file.MyListEntry);

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

    private void UpdateGenericFile(AniDbFileResult result)
    {
        var eGenericFile = _context.AniDbGenericFiles.Include(f => f.MyListEntry)
            .SingleOrDefault(f => f.Id == result.FileId);
        var genericFile = new AniDbGenericFile
        {
            Id = result.FileId,
            AniDbEpisodeId = result.EpisodeId!.Value,
            MyListEntryId = result.MyListId,
            MyListEntry = result.MyListId is null ? null : FileResultToAniDbMyListEntry(result)
        };

        UpdateMyListEntry(genericFile.MyListEntry);
        
        if (eGenericFile is null)
            _context.Entry(genericFile).State = EntityState.Added;
        else
            _context.Entry(eGenericFile).CurrentValues.SetValues(genericFile);
        _context.SaveChanges();
    }

    private void UpdateMyListEntry(AniDbMyListEntry? entry)
    {
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

    private void UpdateEpRelations(AniDbFileResult result)
    {
        var eRels = _context.AniDbEpisodeFileXrefs.Where(x => x.AniDbFileId == result.FileId).ToList();
        var rels = (result.OtherEpisodeIds ?? new List<int>()).Append(result.EpisodeId!.Value)
            .Select(x => new AniDbEpisodeFileXref { AniDbEpisodeId = x, AniDbFileId = result.FileId }).ToList();

        _context.AniDbEpisodeFileXrefs.RemoveRange(eRels.ExceptBy(rels.Select(x => x.AniDbEpisodeId), x => x.AniDbEpisodeId));
        _context.AniDbEpisodeFileXrefs.AddRange(rels.ExceptBy(eRels.Select(x => x.AniDbEpisodeId), x => x.AniDbEpisodeId));

        _context.SaveChanges();
    }

    private async Task<AniDbFileResult?> GetFileResult()
    {
        // Check if file was requested before and did not complete
        var result = await _fileResultCache.Get(_fileResultCacheKey);
        if (result is not null)
            return result;

        FileRequest? fileReq;
        switch (CommandArgs.IdType)
        {
            case IdType.LocalId:
            {
                var localFile = _context.LocalFiles.Find(CommandArgs.Id);
                if (localFile is null)
                {
                    Completed = true;
                    _logger.LogWarning("Unable to process local file id: {LocalFileId} not found, skipping", CommandArgs.Id);
                    return null;
                }
                _logger.LogInformation("Processing local file id: {LocalfileId}, ed2k: {LocalFileEd2k}", CommandArgs.Id, localFile.Ed2K);
                fileReq = _udpRequestFactory.FileRequest(localFile.FileSize, localFile.Ed2K, FileRequest.DefaultFMask, FileRequest.DefaultAMask);
                break;
            }
            case IdType.FileId:
                _logger.LogInformation("Processing file id: {FileId}", CommandArgs.Id);
                fileReq = _udpRequestFactory.FileRequest(CommandArgs.Id, FileRequest.DefaultFMask, FileRequest.DefaultAMask);
                break;
            default:
                throw new ArgumentException("Idtype does not exist");
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
            await _fileResultCache.Save(_fileResultCacheKey, result);
        }
        return result;
    }
}
