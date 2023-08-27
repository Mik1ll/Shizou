using System;
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
                first.Viewdate is not null ? DateTimeOffset.Parse(first.Viewdate) : null);
        }).ToList();
    }

    private void UpdateFileStates(List<MyListItem> myListItems)
    {
        var dbFiles = _context.FileWatchedStates.Select(f => new { f.Id, f.Watched, f.WatchedUpdated })
            .Union(from gf in _context.AniDbGenericFiles
                join e in _context.EpisodeWatchedStates
                    on gf.AniDbEpisodeId equals e.Id
                select new { gf.Id, e.Watched, e.WatchedUpdated }).ToDictionary(f => f.Id);
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
                var expectedState = dbFilesWithLocal.Contains(dbFile.Id) ? _options.MyList.PresentFileState : _options.MyList.AbsentFileState;
                var itemWatched = item.Viewdate is not null;
                var updateWatched = dbFile.WatchedUpdated is not null &&
                                    DateOnly.FromDateTime(dbFile.WatchedUpdated.Value) >= item.Updated
                                    && dbFile.Watched != itemWatched;
                if (updateWatched)
                    toUpdate.Add(new UpdateMyListArgs(true, expectedState, dbFile.Watched, dbFile.WatchedUpdated, item.Id));
                else if (item.State != expectedState || item.FileState != MyListFileState.Normal)
                    toUpdate.Add(new UpdateMyListArgs(true, expectedState, Lid: item.Id));
                if (!updateWatched && dbFile.Watched != itemWatched)
                {
                    var fileWatchedState = _context.FileWatchedStates.Find(dbFile.Id);
                    if (fileWatchedState is not null)
                    {
                        fileWatchedState.Watched = itemWatched;
                        fileWatchedState.WatchedUpdated = null;
                    }
                    else
                    {
                        var episodeWatchedState = (from gf in _context.AniDbGenericFiles
                            join ep in _context.AniDbEpisodes
                                on gf.AniDbEpisodeId equals ep.Id
                            join ws in _context.EpisodeWatchedStates
                                on ep.Id equals ws.Id
                            where gf.Id == dbFile.Id
                            select ws).FirstOrDefault();
                        if (episodeWatchedState is not null)
                        {
                            episodeWatchedState.Watched = itemWatched;
                            episodeWatchedState.WatchedUpdated = null;
                        }
                        else
                        {
                            throw new InvalidOperationException("Tried to update the watched state of a missing file or generic file with a missing episode");
                        }
                    }
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
            var updateItems = (from u in toUpdate
                join i in items
                    on u.Lid equals i.Id
                select new { u, i }).ToList();
            var itemsWithoutUpdates = items.Except(updateItems.Select(ui => ui.i)).ToList();
            if (updateItems.Count <= 1)
                continue;
            var firstUpdate = updateItems.First().u;
            var newStates = updateItems.Select(ui => new
                {
                    State = ui.u.MyListState!.Value, Watched = ui.u.Watched ?? ui.i.Viewdate is not null,
                    WatchedDate = ui.u.Watched is null ? ui.i.Viewdate : ui.u.WatchedDate
                })
                .Concat(itemsWithoutUpdates.Select(i => new { i.State, Watched = i.Viewdate is not null, WatchedDate = i.Viewdate })).ToList();
            if (newStates.All(s => s.State == firstUpdate.MyListState) && updateItems.All(ui => ui.u.Watched is null))
            {
                foreach (var ui in updateItems)
                    toUpdate.Remove(ui.u);
                toUpdate.Add(new UpdateMyListArgs(true, firstUpdate.MyListState, Aid: aGroup.Key, EpNo: "0"));
            }
            else if (newStates.All(s => s.State == firstUpdate.MyListState && s.Watched == firstUpdate.Watched && s.WatchedDate == firstUpdate.WatchedDate))
            {
                foreach (var ui in updateItems)
                    toUpdate.Remove(ui.u);
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