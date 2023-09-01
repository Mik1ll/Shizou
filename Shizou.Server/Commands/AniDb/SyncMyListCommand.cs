﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Shizou.Data;
using Shizou.Data.Database;
using Shizou.Data.Enums;
using Shizou.Data.Models;
using Shizou.Server.AniDbApi.Requests.Http;
using Shizou.Server.Options;
using Shizou.Server.Services;

// using System.Xml;
// using System.Xml.Serialization;

namespace Shizou.Server.Commands.AniDb;

public record SyncMyListArgs() : CommandArgs($"{nameof(SyncMyListCommand)}");

[Command(CommandType.SyncMyList, CommandPriority.Low, QueueType.AniDbHttp)]
public class SyncMyListCommand : BaseCommand<SyncMyListArgs>
{
    private readonly ILogger<SyncMyListCommand> _logger;
    private readonly ShizouContext _context;
    private readonly CommandService _commandService;
    private readonly HttpRequestFactory _httpRequestFactory;
    private readonly ShizouOptions _options;

    private record MyListItem(MyListState State, MyListFileState FileState, int Id, HashSet<int> Aids, HashSet<int> Eids, int Fid, DateOnly Updated,
        DateTimeOffset? Viewdate);

    public SyncMyListCommand(
        ILogger<SyncMyListCommand> logger,
        ShizouContext context,
        CommandService commandService,
        IOptionsSnapshot<ShizouOptions> options,
        HttpRequestFactory httpRequestFactory
    )
    {
        _logger = logger;
        _context = context;
        _commandService = commandService;
        _httpRequestFactory = httpRequestFactory;
        _options = options.Value;
    }

    public TimeSpan MyListRequestPeriod { get; } = TimeSpan.FromHours(24);

    protected override async Task ProcessInner()
    {
        var myListResult = await GetMyList();

        if (myListResult is null)
        {
            Completed = true;
            return;
        }

        var myListItems = MyListResultToMyListItems(myListResult);

        UpdateFileStates(myListItems);

        _commandService.Dispatch(new AddMissingMyListEntriesArgs());

        Completed = true;
    }

    private static List<MyListItem> MyListResultToMyListItems(MyListResult myListResult)
    {
        return myListResult.MyListItems.GroupBy(i => i.Id).Select(g =>
        {
            var list = g.ToList();
            var first = list.First();
            return new MyListItem(first.State, first.FileState, first.Id, list.Select(i => i.Aid).ToHashSet(),
                list.Select(i => i.Eid).ToHashSet(), first.Fid, DateOnly.ParseExact(first.Updated, "yyyy-MM-dd"),
                first.Viewdate is null ? null : DateTimeOffset.Parse(first.Viewdate));
        }).ToList();
    }

    private void UpdateFileStates(List<MyListItem> myListItems)
    {
        var dbFiles = _context.FileWatchedStates.Select(ws => new { FileId = ws.Id, WatchedState = (IWatchedState)ws }).ToList()
            .Union((from gf in _context.AniDbGenericFiles
                join ws in _context.EpisodeWatchedStates
                    on gf.AniDbEpisodeId equals ws.Id
                select new { FileId = gf.Id, WatchedState = (IWatchedState)ws }).ToList()).ToDictionary(f => f.FileId);
        var dbFilesWithLocal = _context.FilesWithLocal.Select(f => f.Id)
            .Union(_context.GenericFilesWithManualLinks.Select(f => f.Id)).ToHashSet();
        var dbEpIdsWithoutGenericFile = (from e in _context.AniDbEpisodes
            where !_context.AniDbGenericFiles.Any(gf => gf.AniDbEpisodeId == e.Id)
            select e.Id).ToHashSet();

        List<UpdateMyListArgs> toUpdate = new();
        List<ProcessArgs> toProcess = new();

        // case 1: matching file id with local regular file
        //         sync with file watch state and file state with present if there are local files
        // case 2: matching file id with local generic file
        //         sync with episode watch state and file state with present if there are manual links
        // case 3: file id doesn't match any local file/generic file
        //         3a: it only has one episode relation and matching local episode that does not have a generic file
        //             don't update it, dispatch a process command
        //         3b: default
        //             don't update watch state, file state to absent

        foreach (var item in myListItems)
            if (dbFiles.TryGetValue(item.Fid, out var dbFile))
            {
                var watchedState = dbFile.WatchedState;
                var expectedState = dbFilesWithLocal.Contains(dbFile.FileId) ? _options.MyList.PresentFileState : _options.MyList.AbsentFileState;
                var itemWatched = item.Viewdate is not null;
                var updateWatched = watchedState.WatchedUpdated is not null &&
                                    DateOnly.FromDateTime(watchedState.WatchedUpdated.Value) >= item.Updated
                                    && watchedState.Watched != itemWatched;
                if (updateWatched)
                    toUpdate.Add(new UpdateMyListArgs(true, expectedState, watchedState.Watched, watchedState.WatchedUpdated, item.Id));
                else if (item.State != expectedState || item.FileState != MyListFileState.Normal)
                    toUpdate.Add(new UpdateMyListArgs(true, expectedState, Lid: item.Id));

                watchedState.MyListId = item.Id;
                if (!updateWatched && watchedState.Watched != itemWatched)
                {
                    watchedState.Watched = itemWatched;
                    watchedState.WatchedUpdated = null;
                }
            }
            else
            {
                if (item.Eids.Count == 1 && dbEpIdsWithoutGenericFile.Contains(item.Eids.First()))
                    toProcess.Add(new ProcessArgs(item.Fid, IdTypeLocalFile.FileId));
                else if (item.State != _options.MyList.AbsentFileState || item.FileState != MyListFileState.Normal)
                    toUpdate.Add(new UpdateMyListArgs(true, _options.MyList.AbsentFileState, Lid: item.Id));
            }

        _context.SaveChanges();

        CombineUpdates(myListItems, toUpdate);

        _commandService.DispatchRange(toUpdate);
        _commandService.DispatchRange(toProcess);
    }

    private static void CombineUpdates(List<MyListItem> myListItems, List<UpdateMyListArgs> toUpdate)
    {
        foreach (var aGroup in from i in myListItems
                 group i by i.Aids.First()
                 into aGroup
                 where aGroup.All(i => i.Aids.Count == 1)
                 select aGroup)
        {
            var items = aGroup.ToList();
            var updateItems = (from i in items
                join u in toUpdate
                    on i.Id equals u.Lid into lj
                select new { i, u = lj.SingleOrDefault() }).ToList();
            var updates = updateItems.Where(ui => ui.u is not null).Select(ui => ui.u).ToList();
            if (updates.Count <= 1)
                continue;
            var newStates = updateItems.Select(ui => new
            {
                State = ui.u?.MyListState!.Value ?? ui.i.State, Watched = ui.u?.Watched ?? ui.i.Viewdate is not null,
                WatchedDate = ui.u?.Watched is null ? ui.i.Viewdate : ui.u.WatchedDate
            }).ToList();
            var firstUpdate = updates.First();
            if (newStates.All(s => s.State == firstUpdate.MyListState) && updates.All(u => u.Watched is null))
            {
                foreach (var u in updates)
                    toUpdate.Remove(u);
                toUpdate.Add(new UpdateMyListArgs(true, firstUpdate.MyListState, Aid: aGroup.Key, EpNo: "0"));
            }
            else if (newStates.All(s => s.State == firstUpdate.MyListState && s.Watched == firstUpdate.Watched && s.WatchedDate == firstUpdate.WatchedDate))
            {
                foreach (var u in updates)
                    toUpdate.Remove(u);
                toUpdate.Add(new UpdateMyListArgs(true, firstUpdate.MyListState, firstUpdate.Watched, firstUpdate.WatchedDate, Aid: aGroup.Key, EpNo: "0"));
            }
        }
    }

    private async Task<MyListResult?> GetMyList()
    {
        // return new XmlSerializer(typeof(MyListResult)).Deserialize(new XmlTextReader(FilePaths.MyListPath)) as MyListResult;
        var requestable = true;
        var fileInfo = new FileInfo(FilePaths.MyListPath);
        if (fileInfo.Exists)
            requestable = DateTime.UtcNow - fileInfo.LastWriteTimeUtc > MyListRequestPeriod;
        if (!requestable)
        {
            _logger.LogWarning("Failed to get mylist: already requested in last {Hours} hours", MyListRequestPeriod.TotalHours);
            return null;
        }

        var request = _httpRequestFactory.MyListRequest();
        await request.Process();
        if (request.MyListResult is null)
        {
            if (!File.Exists(FilePaths.MyListPath))
                await File.Create(FilePaths.MyListPath).DisposeAsync();
            File.SetLastWriteTimeUtc(FilePaths.MyListPath, DateTime.UtcNow);
            _logger.LogWarning("Failed to get mylist data from AniDb, retry in {Hours} hours", MyListRequestPeriod.TotalHours);
        }
        else
        {
            _logger.LogDebug("Overwriting mylist file");
            Directory.CreateDirectory(Path.GetDirectoryName(FilePaths.MyListPath)!);
            await File.WriteAllTextAsync(FilePaths.MyListPath, request.ResponseText, Encoding.UTF8);
            var backupFilePath = Path.Combine(FilePaths.MyListBackupDir, DateTime.UtcNow.ToString("yyyy-MM-dd") + ".xml");
            Directory.CreateDirectory(FilePaths.MyListBackupDir);
            await File.WriteAllTextAsync(backupFilePath, request.ResponseText, Encoding.UTF8);
            _logger.LogInformation("HTTP Get mylist succeeded");
        }

        return request.MyListResult;
    }
}