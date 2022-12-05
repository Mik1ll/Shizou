﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Shizou.AniDbApi;
using Shizou.AniDbApi.Requests;
using Shizou.AniDbApi.Results;
using Shizou.CommandProcessors;
using Shizou.Database;
using Shizou.Models;

namespace Shizou.Commands.AniDb;

public sealed record ProcessParams(int LocalFileId) : CommandParams($"{nameof(ProcessCommand)}_{LocalFileId}");

[Command(CommandType.GetFile, CommandPriority.Default, QueueType.AniDbUdp)]
public class ProcessCommand : BaseCommand<ProcessParams>
{
    private readonly CommandManager _cmdMgr;
    private readonly ShizouContext _context;
    private readonly string _fileCachePath;

    public ProcessCommand(IServiceProvider provider, ProcessParams commandParams)
        : base(provider, provider.GetRequiredService<ILogger<ProcessCommand>>(), commandParams)
    {
        _context = provider.GetRequiredService<ShizouContext>();
        _cmdMgr = provider.GetRequiredService<CommandManager>();
        _fileCachePath = Path.Combine(Constants.TempFilePath, CommandParams.CommandId + ".json");
    }

    public override async Task Process()
    {
        Logger.LogInformation("Processing local file id: {localfileId}", CommandParams.LocalFileId);
        var localFile = _context.LocalFiles.Find(CommandParams.LocalFileId);
        if (localFile is null)
        {
            Completed = true;
            Logger.LogWarning("Unable to process local file id: {localFileId} not found, skipping", CommandParams.LocalFileId);
            return;
        }

        var result = await GetFileResult(localFile);
        if (result is null)
            return;

        UpdateDatabase(result);

        _cmdMgr.Dispatch(new HttpAnimeParams(result.AnimeId!.Value));

        Completed = true;
        File.Delete(_fileCachePath);
    }

    private void UpdateDatabase(AniDbFileResult result)
    {
        // Get the group
        var group = _context.AniDbGroups.Find(result.GroupId);
        var newGroup = new AniDbGroup(result);
        if (group is null)
            _context.AniDbGroups.Add(newGroup);
        else
            _context.Entry(group).CurrentValues.SetValues(newGroup);

        // Get the file
        var file = _context.AniDbFiles
            .Include(f => f.MyListEntry)
            .FirstOrDefault(f => f.Id == result.FileId);
        var newFile = new AniDbFile(result);
        if (file is null)
            _context.AniDbFiles.Add(newFile);
        else
        {
            _context.Entry(file).CurrentValues.SetValues(newFile);
            _context.ReplaceNavigationList(newFile.Audio, file.Audio);
            _context.ReplaceNavigationList(newFile.Subtitles, file.Subtitles);
        }
        _context.SaveChanges();
        UpdateEpRelations(result);
    }

    private void UpdateEpRelations(AniDbFileResult result)
    {
        var epIds = new List<int> { result.EpisodeId!.Value };
        epIds.AddRange(result.OtherEpisodeIds!);
        var deleteRels = _context.AniDbEpisodeFileXrefs.Where(x => x.AniDbFileId == result.FileId);
        _context.RemoveRange(deleteRels);
        var addRels = epIds.Select(x => new AniDbEpisodeFileXref { AniDbEpisodeId = x, AniDbFileId = result.FileId });
        _context.AddRange(addRels);
        _context.SaveChanges();
    }

    private async Task<AniDbFileResult?> GetFileResult(LocalFile localFile)
    {
        // Check if file was requested before and did not complete
        var result = await GetFromFileCache();
        if (result is not null)
            return result;

        var fileReq = new FileRequest(Provider, localFile.FileSize, localFile.Ed2K, FileRequest.DefaultFMask, FileRequest.DefaultAMask);
        await fileReq.Process();
        result = fileReq.FileResult;
        if (fileReq.ResponseCode == AniDbResponseCode.NoSuchFile)
        {
            Logger.LogInformation("Skipped processing local file id: {localFileId}, ed2k: {localFileEd2k}, file not found on anidb", localFile.Id,
                localFile.Ed2K);
            Completed = true;
        }
        else if (result is null)
        {
            Logger.LogError("Could not process local file id: {localFileId}, ed2k: {localFileEd2k}, no file result", localFile.Id, localFile.Ed2K);
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
        if (!Directory.Exists(Constants.TempFilePath))
            Directory.CreateDirectory(Constants.TempFilePath);
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
