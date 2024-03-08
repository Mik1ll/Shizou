using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Shizou.Data;
using Shizou.Data.CommandInputArgs;
using Shizou.Data.Database;
using Shizou.Data.Enums;
using Shizou.Data.Models;
using Shizou.Server.AniDbApi.Requests.Http;
using Shizou.Server.AniDbApi.Requests.Http.Interfaces;
using Shizou.Server.Options;
using Shizou.Server.Services;

namespace Shizou.Server.Commands.AniDb;

public class SyncMyListCommand : Command<SyncMyListArgs>
{
    private readonly CommandService _commandService;
    private readonly IShizouContext _context;
    private readonly ILogger<SyncMyListCommand> _logger;
    private readonly IMyListRequest _myListRequest;
    private readonly TimeSpan _myListRequestPeriod = TimeSpan.FromHours(24);
    private readonly ShizouOptions _options;

    public SyncMyListCommand(
        ILogger<SyncMyListCommand> logger,
        IShizouContext context,
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

    protected override async Task ProcessInnerAsync()
    {
        if (_options.AniDb.MyList.DisableSync)
        {
            _logger.LogWarning("Sync command disabled in settings, skipping");
            Completed = true;
            return;
        }

        _logger.LogInformation("Starting mylist sync, Present State: {PresentState}, Absent State: {AbsentState}",
            _options.AniDb.MyList.PresentFileState, _options.AniDb.MyList.AbsentFileState);

        var myListResult = await GetMyListAsync().ConfigureAwait(false);

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
        var eFileIds = _context.AniDbFiles.Select(f => f.Id).ToHashSet();
        var eEpIds = _context.AniDbEpisodes.Select(e => e.Id).ToHashSet();
        var animeToAdd = new HashSet<int>();
        var eRelsLkup = _context.AniDbEpisodeFileXrefs.AsNoTracking().ToLookup(xref => xref.AniDbFileId);
        var eHangingRelsLkup = _context.HangingEpisodeFileXrefs.AsNoTracking().ToLookup(xref => xref.AniDbFileId);
        foreach (var item in myListItems.Where(i => eFileIds.Contains(i.Fid)))
        {
            var eRels = eRelsLkup[item.Fid].ToList();
            var eHangingRels = eHangingRelsLkup[item.Fid].ToList();
            _context.AniDbEpisodeFileXrefs.RemoveRange(eRels.ExceptBy(item.Eids, x => x.AniDbEpisodeId));
            _context.HangingEpisodeFileXrefs.RemoveRange(eHangingRels.ExceptBy(item.Eids, x => x.AniDbEpisodeId));
            foreach (var relEid in item.Eids
                         .Except(eRels.Select(x => x.AniDbEpisodeId))
                         .Except(eHangingRels.Select(x => x.AniDbEpisodeId)))
                if (eEpIds.Contains(relEid))
                    _context.AniDbEpisodeFileXrefs.Add(new AniDbEpisodeFileXref
                    {
                        AniDbEpisodeId = relEid,
                        AniDbFileId = item.Fid
                    });
                else
                    _context.HangingEpisodeFileXrefs.Add(new HangingEpisodeFileXref
                    {
                        AniDbEpisodeId = relEid,
                        AniDbFileId = item.Fid
                    });
            foreach (var aid in item.Aids)
                if (!eAnimeIds.Contains(aid))
                    animeToAdd.Add(aid);
        }

        _context.SaveChanges();

        _commandService.DispatchRange(animeToAdd.Select(aid => new AnimeArgs(aid)));
    }

    [SuppressMessage("ReSharper.DPA", "DPA0007: Large number of DB records", MessageId = "count: 2000")]
    private void UpdateFileStates(List<MyListItem> myListItems)
    {
        // Get watched states for files and generic files, episode watch states without a file id excluded
        var watchedStatesByFileId = _context.FileWatchedStates.Select(ws => new { FileId = ws.AniDbFileId, WatchedState = (IWatchedState)ws }).ToList()
            .Union((from ws in _context.EpisodeWatchedStates
                where ws.AniDbFileId != null
                select new { FileId = ws.AniDbFileId!.Value, WatchedState = (IWatchedState)ws }).ToList()).ToDictionary(f => f.FileId, f => f.WatchedState);

        // Get file id and generic file ids for files with local files
        var fileIdsWithLocal = _context.AniDbFiles.Where(f => f.LocalFile != null).Select(f => f.Id)
            .Union(_context.EpisodeWatchedStates.Where(ws => ws.AniDbFileId != null && ws.AniDbEpisode.ManualLinkLocalFiles.Any())
                .Select(ws => ws.AniDbFileId!.Value)).ToHashSet();

        // Get episode ids that do not have a generic file id, and have manual links
        var epIdsMissingGenericFile = (from ws in _context.EpisodeWatchedStates
            where ws.AniDbFileId == null && ws.AniDbEpisode.ManualLinkLocalFiles.Any()
            select ws.AniDbEpisodeId).ToHashSet();

        List<UpdateMyListArgs> toUpdate = [];
        List<ProcessArgs> toProcess = [];

        foreach (var item in myListItems)
        {
            var expectedState = fileIdsWithLocal.Contains(item.Fid) ? _options.AniDb.MyList.PresentFileState : _options.AniDb.MyList.AbsentFileState;
            var itemWatched = item.Viewdate is not null;
            var hasWatchedStateWithFileId = watchedStatesByFileId.TryGetValue(item.Fid, out var watchedState);
            var fileMayBeGeneric = !hasWatchedStateWithFileId && item.Eids.Count == 1 && epIdsMissingGenericFile.Contains(item.Eids.First());
            var updateQueued = false;

            if (hasWatchedStateWithFileId)
            {
                if (watchedState!.Watched != itemWatched)
                {
                    if (watchedState.WatchedUpdated is not null &&
                        DateOnly.FromDateTime(watchedState.WatchedUpdated.Value) >= item.Updated)
                    {
                        toUpdate.Add(new UpdateMyListArgs(true, expectedState, watchedState.Watched, watchedState.WatchedUpdated, item.Id));
                        updateQueued = true;
                    }
                    else
                    {
                        watchedState.Watched = itemWatched;
                        watchedState.WatchedUpdated = null;
                    }
                }

                watchedState.MyListId = item.Id;
            }
            else if (fileMayBeGeneric)
            {
                // Check if file in mylist is a generic file for an episode with manual links
                toProcess.Add(new ProcessArgs(item.Fid, IdTypeLocalOrFile.FileId));
            }

            if (!updateQueued && !fileMayBeGeneric && (item.State != expectedState || item.FileState != MyListFileState.Normal))
                toUpdate.Add(new UpdateMyListArgs(true, expectedState, Lid: item.Id));
        }

        _context.SaveChanges();

        CombineUpdates(myListItems, toUpdate);

        _commandService.DispatchRange(toUpdate);
        _commandService.DispatchRange(toProcess);
    }

    private async Task<MyListResult?> GetMyListAsync()
    {
        // return new XmlSerializer(typeof(MyListResult)).Deserialize(new XmlTextReader(FilePaths.MyListPath)) as MyListResult;
        var timer = _context.Timers.FirstOrDefault(t => t.Type == TimerType.MyListRequest);
        if (timer is not null && timer.Expires > DateTime.UtcNow)
        {
            _logger.LogWarning("Failed to get mylist: already requested in last {Hours} hours", _myListRequestPeriod.TotalHours);
            return null;
        }

        var expires = DateTime.UtcNow + _myListRequestPeriod;
        if (timer is not null)
            timer.Expires = expires;
        else
            _context.Timers.Add(new Timer
            {
                Type = TimerType.MyListRequest,
                Expires = expires
            });

        _context.SaveChanges();

        _logger.LogInformation("Sending HTTP mylist request");
        _myListRequest.SetParameters();
        await _myListRequest.ProcessAsync().ConfigureAwait(false);
        if (_myListRequest.MyListResult is null)
        {
            _logger.LogWarning("Failed to get mylist data from AniDB, retry in {Hours} hours", _myListRequestPeriod.TotalHours);
        }
        else
        {
            _logger.LogDebug("Saving mylist file to \"{MyListPath}\"", FilePaths.MyListPath);
            Directory.CreateDirectory(Path.GetDirectoryName(FilePaths.MyListPath)!);
            await File.WriteAllTextAsync(FilePaths.MyListPath, _myListRequest.ResponseText, Encoding.UTF8).ConfigureAwait(false);
            var backupFilePath = Path.Combine(FilePaths.MyListBackupDir, DateTime.UtcNow.ToString("yyyy-MM-dd") + ".xml");
            _logger.LogDebug("Saving mylist backup to \"{MyListBackupPath}\"", backupFilePath);
            Directory.CreateDirectory(FilePaths.MyListBackupDir);
            await File.WriteAllTextAsync(backupFilePath, _myListRequest.ResponseText, Encoding.UTF8).ConfigureAwait(false);
            _logger.LogInformation("HTTP Get mylist succeeded");
        }

        return _myListRequest.MyListResult;
    }

    private record MyListItem(
        MyListState State,
        MyListFileState FileState,
        int Id,
        HashSet<int> Aids,
        HashSet<int> Eids,
        int Fid,
        DateOnly Updated,
        DateTimeOffset? Viewdate);
}
