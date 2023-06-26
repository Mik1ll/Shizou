using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Shizou.Common.Enums;
using Shizou.Data.Database;
using Shizou.Data.Models;
using Shizou.Server.AniDbApi.Requests.Udp;
using Shizou.Server.AniDbApi.Requests.Udp.Results;
using Shizou.Server.Services;
using Shizou.Server.Services.FileCaches;

namespace Shizou.Server.Commands.AniDb;

public enum IdType
{
    LocalId = 1,
    FileId = 2
}

public sealed record ProcessArgs(int Id, IdType IdType) : CommandArgs($"{nameof(ProcessCommand)}_idtype={IdType}");

[Command(CommandType.GetFile, CommandPriority.Normal, QueueType.AniDbUdp)]
public class ProcessCommand : BaseCommand<ProcessArgs>
{
    private readonly CommandService _commandService;
    private readonly ILogger<ProcessCommand> _logger;
    private readonly ShizouContext _context;
    private readonly string _fileResultCacheKey;
    private readonly AniDbFileResultCache _fileResultCache;
    private readonly IServiceProvider _provider;

    public ProcessCommand(
        ProcessArgs commandArgs,
        ILogger<ProcessCommand> logger,
        ShizouContext context,
        CommandService commandService,
        AniDbFileResultCache fileResultCache,
        IServiceProvider provider
    ) : base(commandArgs)
    {
        _logger = logger;
        _context = context;
        _commandService = commandService;
        _fileResultCache = fileResultCache;
        _provider = provider;
        _fileResultCacheKey = $"File_{CommandArgs.Id}.json";
    }

    public override async Task Process()
    {
        var result = await GetFileResult();

        if (result is null)
            return;

        UpdateDatabase(result);

        _commandService.Dispatch(new AnimeArgs(result.AnimeId!.Value));

        Completed = true;
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
        var file = _context.AniDbFiles
            .Include(f => f.AniDbGroup)
            .Include(f => f.MyListEntry)
            .SingleOrDefault(f => f.Id == result.FileId);
        var newFile = new AniDbFile
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

        if (newFile.MyListEntry is not null)
        {
            var eMyListEntry = _context.AniDbMyListEntries.Find(newFile.MyListEntryId);
            if (eMyListEntry is null)
                _context.AniDbMyListEntries.Add(newFile.MyListEntry);
            else
                _context.Entry(eMyListEntry).CurrentValues.SetValues(newFile.MyListEntry);
        }
        if (file?.MyListEntry is not null && file.MyListEntryId != newFile.MyListEntryId)
            _context.AniDbMyListEntries.Remove(file.MyListEntry);
        _context.SaveChanges();

        if (newFile.AniDbGroup is not null)
        {
            var eAniDbGroup = _context.AniDbGroups.Find(newFile.AniDbGroupId);
            if (eAniDbGroup is null)
                _context.AniDbGroups.Add(newFile.AniDbGroup);
            else
                _context.Entry(eAniDbGroup).CurrentValues.SetValues(newFile.AniDbGroup);
        }
        _context.SaveChanges();


        if (file is null)
        {
            _context.Entry(newFile).State = EntityState.Added;
        }
        else
        {
            _context.ReplaceList(newFile.Audio, file.Audio, a => a.Id);
            _context.ReplaceList(newFile.Subtitles, file.Subtitles, s => s.Id);
            _context.Entry(file).CurrentValues.SetValues(newFile);
        }
        _context.SaveChanges();

        UpdateEpRelations(result);
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

    private void UpdateGenericFile(AniDbFileResult result)
    {
        var genericFile = _context.AniDbGenericFiles.Include(f => f.MyListEntry)
            .SingleOrDefault(f => f.Id == result.FileId);
        var newGenericFile = new AniDbGenericFile
        {
            Id = result.FileId,
            AniDbEpisodeId = result.EpisodeId!.Value,
            MyListEntryId = result.MyListId,
            MyListEntry = result.MyListId is null ? null : FileResultToAniDbMyListEntry(result)
        };
        if (newGenericFile.MyListEntry is not null)
        {
            var eMyListEntry = _context.AniDbMyListEntries.Find(newGenericFile.MyListEntryId);
            if (eMyListEntry is null)
                _context.AniDbMyListEntries.Add(newGenericFile.MyListEntry);
            else
                _context.Entry(eMyListEntry).CurrentValues.SetValues(newGenericFile.MyListEntry);
        }
        if (genericFile?.MyListEntry is not null && genericFile.MyListEntryId != newGenericFile.MyListEntryId)
            _context.AniDbMyListEntries.Remove(genericFile.MyListEntry);
        _context.SaveChanges();

        if (genericFile is null)
            _context.Entry(newGenericFile).State = EntityState.Added;
        else
            _context.Entry(genericFile).CurrentValues.SetValues(newGenericFile);
        _context.SaveChanges();
    }

    private void UpdateEpRelations(AniDbFileResult result)
    {
        var resultRels = (result.OtherEpisodeIds ?? new List<int>()).Append(result.EpisodeId!.Value)
            .Select(x => new AniDbEpisodeFileXref { AniDbEpisodeId = x, AniDbFileId = result.FileId }).ToList();
        var dbRels = _context.AniDbEpisodeFileXrefs.Where(x => x.AniDbFileId == result.FileId).ToList();
        _context.ReplaceList(resultRels, dbRels, r => r.AniDbEpisodeId);

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
                fileReq = new FileRequest(_provider, localFile.FileSize, localFile.Ed2K, FileRequest.DefaultFMask, FileRequest.DefaultAMask);
                break;
            }
            case IdType.FileId:
                _logger.LogInformation("Processing file id: {FileId}", CommandArgs.Id);
                fileReq = new FileRequest(_provider, CommandArgs.Id, FileRequest.DefaultFMask, FileRequest.DefaultAMask);
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
