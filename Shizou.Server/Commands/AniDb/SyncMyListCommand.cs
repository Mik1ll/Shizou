using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
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
using Shizou.Server.AniDbApi.Requests.Http.Interfaces;
using Shizou.Server.Options;
using Shizou.Server.Services;

// using System.Xml;
// using System.Xml.Serialization;

namespace Shizou.Server.Commands.AniDb;

public record SyncMyListArgs() : CommandArgs($"{nameof(SyncMyListCommand)}");

[Command(CommandType.SyncMyList, CommandPriority.Low, QueueType.AniDbHttp)]
public class SyncMyListCommand : Command<SyncMyListArgs>
{
    private readonly CommandService _commandService;
    private readonly ShizouContext _context;
    private readonly ILogger<SyncMyListCommand> _logger;
    private readonly IMyListRequest _myListRequest;
    private readonly TimeSpan _myListRequestPeriod = TimeSpan.FromHours(24);
    private readonly ShizouOptions _options;

    public SyncMyListCommand(
        ILogger<SyncMyListCommand> logger,
        ShizouContext context,
        CommandService commandService,
        IOptionsSnapshot<ShizouOptions> options,
        IMyListRequest myListRequest
    )
    {
        _logger = logger;
        _context = context;
        _commandService = commandService;
        _myListRequest = myListRequest;
        _options = options.Value;
    }

    protected override async Task ProcessInner()
    {
        var myListResult = await GetMyList();

        if (myListResult is null)
        {
            Completed = true;
            return;
        }

        var myListItems = MyListResultToMyListItems(myListResult);

        RelationshipFixup(myListItems);

        UpdateFileStates(myListItems);

        _commandService.Dispatch(new AddMissingMyListEntriesArgs());

        Completed = true;
    }

    private void RelationshipFixup(List<MyListItem> myListItems)
    {
        var eAnimeIds = _context.AniDbAnimes.Select(a => a.Id).ToHashSet();
        var matchedItems = from item in myListItems
            join xref in _context.AniDbEpisodeFileXrefs on item.Fid equals xref.AniDbFileId into eRels
            where eRels.Any()
            // I don't know why this inspection occurs, probably a bug?
            // ReSharper disable once UseWithExpressionToCopyAnonymousObject
            select new { item, eRels };
        var animeToAdd = new HashSet<int>();
        foreach (var pair in matchedItems)
        {
            var rels = pair.item.Eids.Select(eid => new AniDbEpisodeFileXref { AniDbEpisodeId = eid, AniDbFileId = pair.item.Fid }).ToList();
            var eRels = pair.eRels.ToList();

            _context.AniDbEpisodeFileXrefs.RemoveRange(eRels.ExceptBy(rels.Select(x => x.AniDbEpisodeId), x => x.AniDbEpisodeId));
            _context.AniDbEpisodeFileXrefs.AddRange(rels.ExceptBy(eRels.Select(x => x.AniDbEpisodeId), x => x.AniDbEpisodeId));

            foreach (var aid in pair.item.Aids)
                if (!eAnimeIds.Contains(aid))
                    animeToAdd.Add(aid);
        }

        _commandService.DispatchRange(animeToAdd.Select(aid => new AnimeArgs(aid)));
        _context.SaveChanges();
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

    [SuppressMessage("ReSharper.DPA", "DPA0007: Large number of DB records", MessageId = "count: 2000")]
    private void UpdateFileStates(List<MyListItem> myListItems)
    {
        var dbFiles = _context.FileWatchedStates.Select(ws => new { FileId = ws.AniDbFileId, WatchedState = (IWatchedState)ws }).ToList()
            .Union((from ws in _context.EpisodeWatchedStates
                where ws.AniDbFileId != null
                select new { FileId = ws.AniDbFileId!.Value, WatchedState = (IWatchedState)ws }).ToList()).ToDictionary(f => f.FileId);
        var dbFilesWithLocal = _context.AniDbFiles.Where(f => f.LocalFile != null).Select(f => f.Id)
            .Union(_context.EpisodeWatchedStates.Where(ws => ws.AniDbFileId != null && ws.AniDbEpisode.ManualLinkLocalFiles.Any())
                .Select(ws => ws.AniDbFileId!.Value)).ToHashSet();
        var dbEpIdsWithoutGenericFile = (from ws in _context.EpisodeWatchedStates
            where ws.AniDbFileId == null
            select ws.AniDbEpisodeId).ToHashSet();

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
                var expectedState = dbFilesWithLocal.Contains(dbFile.FileId) ? _options.AniDb.MyList.PresentFileState : _options.AniDb.MyList.AbsentFileState;
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
                else if (item.State != _options.AniDb.MyList.AbsentFileState || item.FileState != MyListFileState.Normal)
                    toUpdate.Add(new UpdateMyListArgs(true, _options.AniDb.MyList.AbsentFileState, Lid: item.Id));
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
            requestable = DateTime.UtcNow - fileInfo.LastWriteTimeUtc > _myListRequestPeriod;
        if (!requestable)
        {
            _logger.LogWarning("Failed to get mylist: already requested in last {Hours} hours", _myListRequestPeriod.TotalHours);
            return null;
        }

        _myListRequest.SetParameters();
        await _myListRequest.Process();
        if (_myListRequest.MyListResult is null)
        {
            if (!File.Exists(FilePaths.MyListPath))
                await File.Create(FilePaths.MyListPath).DisposeAsync();
            File.SetLastWriteTimeUtc(FilePaths.MyListPath, DateTime.UtcNow);
            _logger.LogWarning("Failed to get mylist data from AniDb, retry in {Hours} hours", _myListRequestPeriod.TotalHours);
        }
        else
        {
            _logger.LogDebug("Overwriting mylist file");
            Directory.CreateDirectory(Path.GetDirectoryName(FilePaths.MyListPath)!);
            await File.WriteAllTextAsync(FilePaths.MyListPath, _myListRequest.ResponseText, Encoding.UTF8);
            var backupFilePath = Path.Combine(FilePaths.MyListBackupDir, DateTime.UtcNow.ToString("yyyy-MM-dd") + ".xml");
            Directory.CreateDirectory(FilePaths.MyListBackupDir);
            await File.WriteAllTextAsync(backupFilePath, _myListRequest.ResponseText, Encoding.UTF8);
            _logger.LogInformation("HTTP Get mylist succeeded");
        }

        return _myListRequest.MyListResult;
    }

    private record MyListItem(MyListState State, MyListFileState FileState, int Id, HashSet<int> Aids, HashSet<int> Eids, int Fid, DateOnly Updated,
        DateTimeOffset? Viewdate);
}
