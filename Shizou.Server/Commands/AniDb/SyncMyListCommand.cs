using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.IO.Compression;
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
using Shizou.Data.Extensions;
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

    private static List<UpdateMyListByEpisodeArgs> CombineUpdates(List<MyListItem> myListItems, List<UpdateMyListArgs> toUpdate)
    {
        var combinedUpdates = new List<UpdateMyListByEpisodeArgs>();
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
                State = ui.u?.MyListState ?? ui.i.State, Watched = ui.u?.Watched ?? ui.i.Viewdate is not null,
                WatchedDate = ui.u?.Watched is null ? ui.i.Viewdate : ui.u.WatchedDate,
            }).ToList();
            var firstUpdate = updates.First();
            if (newStates.All(s => s.State == firstUpdate.MyListState) && updates.All(u => u.Watched is null))
            {
                foreach (var u in updates)
                    toUpdate.Remove(u);
                combinedUpdates.Add(new UpdateMyListByEpisodeArgs(true, aGroup.Key, "0", firstUpdate.MyListState));
            }
            else if (newStates.All(s => s.State == firstUpdate.MyListState && s.Watched == firstUpdate.Watched && s.WatchedDate == firstUpdate.WatchedDate))
            {
                foreach (var u in updates)
                    toUpdate.Remove(u);
                combinedUpdates.Add(new UpdateMyListByEpisodeArgs(true, aGroup.Key, "0", firstUpdate.MyListState, firstUpdate.Watched,
                    firstUpdate.WatchedDate));
            }
        }

        return combinedUpdates;
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
        var animeToAdd = new HashSet<int>();
        var eAnimeIds = _context.AniDbAnimes.Select(a => a.Id).ToHashSet();
        var eNormalFileIds = _context.AniDbNormalFiles.Select(f => f.Id).ToHashSet();
        var eEpIds = _context.AniDbEpisodes.Select(e => e.Id).ToHashSet();
        var eRelsLkup = _context.AniDbEpisodeFileXrefs.AsNoTracking().ToLookup(xref => xref.AniDbFileId);
        var eHangingRelsLkup = _context.HangingEpisodeFileXrefs.AsNoTracking().ToLookup(xref => xref.AniDbNormalFileId);

        foreach (var item in myListItems.Where(i => eNormalFileIds.Contains(i.Fid)))
        {
            var eRelsForItem = eRelsLkup[item.Fid].ToList();
            var eHangingRelsForItem = eHangingRelsLkup[item.Fid].ToList();
            _context.AniDbEpisodeFileXrefs.RemoveRange(eRelsForItem.ExceptBy(item.Eids, x => x.AniDbEpisodeId));
            _context.HangingEpisodeFileXrefs.RemoveRange(eHangingRelsForItem.ExceptBy(item.Eids, x => x.AniDbEpisodeId));
            foreach (var relEid in item.Eids.Except(eRelsForItem.Select(x => x.AniDbEpisodeId))
                         .Except(eHangingRelsForItem.Select(x => x.AniDbEpisodeId)))
                if (eEpIds.Contains(relEid))
                    _context.AniDbEpisodeFileXrefs.Add(new AniDbEpisodeFileXref
                    {
                        AniDbEpisodeId = relEid,
                        AniDbFileId = item.Fid,
                    });
                else
                    _context.HangingEpisodeFileXrefs.Add(new HangingEpisodeFileXref
                    {
                        AniDbEpisodeId = relEid,
                        AniDbNormalFileId = item.Fid,
                    });

            foreach (var aid in item.Aids.Where(aid => !eAnimeIds.Contains(aid)))
                animeToAdd.Add(aid);
        }

        _context.SaveChanges();

        _commandService.Dispatch(animeToAdd.Select(aid => new AnimeArgs(aid)));
    }

    [SuppressMessage("ReSharper.DPA", "DPA0007: Large number of DB records", MessageId = "count: 2000")]
    private void UpdateFileStates(List<MyListItem> myListItems)
    {
        // Get watched states for files and generic files, episode watch states without a file id excluded
        var watchedStatesByFileId = _context.FileWatchedStates.ToDictionary(ws => ws.AniDbFileId);

        // Get file id and generic file ids for files with local files
        var fileIdsWithLocal = _context.AniDbFiles.Where(f => f.LocalFiles.Any()).Select(f => f.Id).ToHashSet();
        var myListItemCountByEpisodeId = myListItems.SelectMany(item => item.Eids).GroupBy(eid => eid).ToDictionary(g => g.Key, g => g.Count());

        // Get episode ids that do not have a generic file id
        var epIdsMissingGenericFile = (from ep in _context.AniDbEpisodes
            where !ep.AniDbFiles.OfType<AniDbGenericFile>().Any()
            select ep.Id).ToHashSet();

        List<UpdateMyListArgs> toUpdate = [];
        List<ProcessArgs> toProcess = [];

        foreach (var item in myListItems)
        {
            var expectedState = fileIdsWithLocal.Contains(item.Fid) ? _options.AniDb.MyList.PresentFileState : _options.AniDb.MyList.AbsentFileState;
            var itemWatched = item.Viewdate is not null;
            var hasWatchedStateWithFileId = watchedStatesByFileId.TryGetValue(item.Fid, out var watchedState);
            var updateQueued = false;

            if (hasWatchedStateWithFileId)
            {
                if (watchedState!.Watched != itemWatched)
                {
                    if (watchedState.WatchedUpdated is not null &&
                        DateOnly.FromDateTime(watchedState.WatchedUpdated.Value) >= item.Updated)
                    {
                        toUpdate.Add(new UpdateMyListArgs(item.Id, expectedState, watchedState.Watched, watchedState.WatchedUpdated));
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
            else if (item.Eids.Count == 1 && // Generic file will always have a single Ep id
                     epIdsMissingGenericFile.Contains(item.Eids.First()) && // We have the local episode missing a generic file
                     myListItemCountByEpisodeId[item.Eids.First()] > 1 // Exclude items whose episode has 1 item associated with it, can be resolved later
                    )
            {
                toProcess.Add(new ProcessArgs(item.Fid, IdTypeLocalOrFile.FileId));
            }

            if (!updateQueued && (item.State != expectedState || item.FileState != MyListFileState.Normal))
                toUpdate.Add(new UpdateMyListArgs(item.Id, expectedState, null, null));
        }

        // Remove my list ids from states without corresponding mylist entry
        var myListIds = myListItems.Select(i => i.Id).ToHashSet();
        foreach (var ws in watchedStatesByFileId.Values.Where(ws => ws.MyListId is not null && !myListIds.Contains(ws.MyListId.Value)))
            ws.MyListId = null;

        _context.SaveChanges();

        var combinedUpdates = CombineUpdates(myListItems, toUpdate);

        _commandService.Dispatch(toUpdate);
        _commandService.Dispatch(toProcess);
        _commandService.Dispatch(combinedUpdates);
    }

    private async Task<MyListResult?> GetMyListAsync()
    {
        // return new XmlSerializer(typeof(MyListResult)).Deserialize(new XmlTextReader(FilePaths.MyListPath)) as MyListResult;
        var timer = _context.Timers.FirstOrDefault(t => t.Type == TimerType.MyListRequest);
        if (timer is not null && timer.Expires > DateTime.UtcNow)
        {
            _logger.LogWarning("Failed to get mylist: already requested, try again after {TimeLeft}", (timer.Expires - DateTime.UtcNow).ToHumanTimeString());
            return null;
        }

        var expires = DateTime.UtcNow + _myListRequestPeriod;
        if (timer is not null)
            timer.Expires = expires;
        else
            _context.Timers.Add(new Timer
            {
                Type = TimerType.MyListRequest,
                Expires = expires,
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
            await CreateMyListBackupAsync().ConfigureAwait(false);
            _logger.LogInformation("HTTP Get mylist succeeded");
        }

        return _myListRequest.MyListResult;
    }

    private async Task CreateMyListBackupAsync()
    {
        _logger.LogDebug("Saving mylist file to \"{MyListPath}\"", FilePaths.MyListPath);
        var myListDirectory = new DirectoryInfo(FilePaths.MyListBackupDir);
        myListDirectory.Create();

        await File.WriteAllTextAsync(FilePaths.MyListPath, _myListRequest.ResponseText, Encoding.UTF8).ConfigureAwait(false);

        // Create timestamped MyList zip archive
        // Backup rotation depeonds on filename being universally sortable ("u" format specifier)
        var archivePath = Path.Join(myListDirectory.FullName, DateTimeOffset.UtcNow.ToString("u").Replace(':', '_') + ".zip");
        _logger.LogDebug("Saving mylist backup to \"{MyListBackupPath}\"", archivePath);
        var backupFs = new FileStream(archivePath, FileMode.OpenOrCreate);
        await using var _ = backupFs.ConfigureAwait(false);
        using var archive = new ZipArchive(backupFs, ZipArchiveMode.Create);
        archive.CreateEntryFromFile(FilePaths.MyListPath, Path.GetFileName(FilePaths.MyListPath));

        // Delete oldest backups when more than 30 exist
        // Only gets zip files that start with ISO 8601 date format YYYY-MM-DD
        var backupFiles = myListDirectory.GetFiles("????-??-?? *.zip").OrderByDescending(f => f.Name).ToList();
        var retainedBackupCount = 30;
        var backUpFilesToDelete = backupFiles.Skip(retainedBackupCount).ToList();
        foreach (var file in backUpFilesToDelete)
            try
            {
                file.Delete();
            }
            catch
            {
                // ignored
            }
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
