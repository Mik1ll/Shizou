using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Shizou.AniDbApi.Requests.Udp;
using Shizou.AniDbApi.Requests.Udp.Results;
using Shizou.CommandProcessors;
using Shizou.Database;
using Shizou.Models;
using Shizou.Services;

namespace Shizou.Commands.AniDb;

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
    private readonly ShizouContext _context;
    private readonly string _fileCachePath;

    public ProcessCommand(IServiceProvider provider, ProcessArgs commandArgs)
        : base(provider, commandArgs)
    {
        _context = provider.GetRequiredService<ShizouContext>();
        _commandService = provider.GetRequiredService<CommandService>();
        _fileCachePath = Path.Combine(Constants.TempFileDir, CommandArgs.CommandId + ".json");
    }

    public override async Task Process()
    {
        var result = await GetFileResult();

        if (result is null)
            return;

        UpdateDatabase(result);

        _commandService.Dispatch(new AnimeArgs(result.AnimeId!.Value));

        Completed = true;
        File.Delete(_fileCachePath);
    }

    private void UpdateDatabase(AniDbFileResult result)
    {
        var file = _context.AniDbFiles
            .Include(f => f.AniDbGroup)
            .Include(f => f.MyListEntry)
            .SingleOrDefault(f => f.Id == result.FileId);
        var newFile = new AniDbFile(result);

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
            _context.Entry(newFile).State = EntityState.Added;
        else
        {
            _context.ReplaceList(newFile.Audio, file.Audio, a => a.Id);
            _context.ReplaceList(newFile.Subtitles, file.Subtitles, s => s.Id);
            _context.Entry(file).CurrentValues.SetValues(newFile);
        }
        _context.SaveChanges();

        UpdateEpRelations(result);
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
        var result = await GetFromFileCache();
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
                    Logger.LogWarning("Unable to process local file id: {localFileId} not found, skipping", CommandArgs.Id);
                    return null;
                }
                Logger.LogInformation("Processing local file id: {localfileId}, ed2k: {localFileEd2k}", CommandArgs.Id, localFile.Ed2K);
                fileReq = new FileRequest(Provider, localFile.FileSize, localFile.Ed2K, FileRequest.DefaultFMask, FileRequest.DefaultAMask);
                break;
            }
            case IdType.FileId:
                Logger.LogInformation("Processing file id: {fileId}", CommandArgs.Id);
                fileReq = new FileRequest(Provider, CommandArgs.Id, FileRequest.DefaultFMask, FileRequest.DefaultAMask);
                break;
            default:
                throw new ArgumentException("Idtype does not exist");
        }

        await fileReq.Process();
        result = fileReq.FileResult;
        if (fileReq.ResponseCode == AniDbResponseCode.NoSuchFile)
        {
            Logger.LogInformation("Skipped processing {idName}: {Id}, file not found on anidb", Enum.GetName(CommandArgs.IdType), CommandArgs.Id);
            Completed = true;
        }
        else if (result is null)
        {
            Logger.LogError("Could not process  {idName}: {Id}, no file result", Enum.GetName(CommandArgs.IdType), CommandArgs.Id);
        }
        else
        {
            // Keep file result just in case command does not complete
            await SaveToFileCache(result);
        }
        return result;
    }

    private async Task SaveToFileCache(AniDbFileResult? result)
    {
        if (!Directory.Exists(Constants.TempFileDir))
            Directory.CreateDirectory(Constants.TempFileDir);
        using (var file = new FileStream(_fileCachePath, FileMode.Create, FileAccess.Write))
        {
            await JsonSerializer.SerializeAsync(file, result);
        }
    }

    private async Task<AniDbFileResult?> GetFromFileCache()
    {
        AniDbFileResult? result = null;
        var fileResult = new FileInfo(_fileCachePath);
        if (fileResult.Exists && fileResult.Length > 0)
            using (var file = new FileStream(_fileCachePath, FileMode.Open, FileAccess.Read))
            {
                result = await JsonSerializer.DeserializeAsync<AniDbFileResult>(file);
            }
        return result;
    }
}
