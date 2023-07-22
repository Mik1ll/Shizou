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
using Shizou.Server.Extensions;
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
        var myListEntries = MyListItemsToMyListEntries(myListItems, DateTime.UtcNow);

        SyncMyListEntries(myListEntries);

        UpdateFileStates(myListItems);

        FindGenericFiles(myListItems);

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

    private static List<AniDbMyListEntry> MyListItemsToMyListEntries(List<MyListItem> items, DateTime updated)
    {
        return items.Select(item => new AniDbMyListEntry
        {
            Id = item.Id,
            FileId = item.Fid,
            Watched = item.Viewdate is not null,
            WatchedDate = item.Viewdate?.UtcDateTime,
            MyListState = item.State,
            MyListFileState = item.FileState,
            Updated = updated
        }).ToList();
    }

    private void FindGenericFiles(List<MyListItem> myListItems)
    {
        var epsWithoutGenericFile = (from e in _context.AniDbEpisodes
            where !_context.AniDbGenericFiles.Any(gf => gf.AniDbEpisodeId == e.Id)
            select e.Id).ToHashSet();
        var anidbFileIds = _context.AniDbFiles.Select(f => f.Id).Union(_context.AniDbGenericFiles.Select(f => f.Id)).ToHashSet();

        // Only retrieve files for episodes we have locally
        var missingFileIds = (from item in myListItems
            where !anidbFileIds.Contains(item.Fid) && item.Eids.Count == 1 && epsWithoutGenericFile.Contains(item.Eids.First())
            select item.Fid).ToHashSet();
        _commandService.DispatchRange(missingFileIds.Select(fid => new ProcessArgs(fid, IdType.FileId)).ToList());
    }

    private void UpdateFileStates(List<MyListItem> myListItems)
    {
        // var animeIdsToMarkAbsent = BulkMarkAbsent(myListItems);
        var dbFiles = _context.AniDbFiles.Select(f => new { f.Id, f.Watched, f.WatchedUpdated })
            .Union(from gf in _context.AniDbGenericFiles
                join e in _context.AniDbEpisodes
                    on gf.AniDbEpisodeId equals e.Id
                select new { gf.Id, e.Watched, e.WatchedUpdated }).ToDictionary(f => f.Id);
        var dbFilesWithLocal = _context.FilesWithLocal.Select(f => f.Id).ToHashSet();
        var dbEpIdsWithoutGenericFile = _context.AniDbEpisodes.Where(e =>
                !_context.AniDbGenericFiles.Any(gf => gf.AniDbEpisodeId == e.Id))
            .Select(e => e.Id).ToHashSet();

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
                var useAniDbWatchedState = dbFile.WatchedUpdated is null || DateOnly.FromDateTime(dbFile.WatchedUpdated.Value) < item.Updated;
                var (syncedWatched, syncedWatchedDateTime) = useAniDbWatchedState
                    ? (item.Viewdate is not null, item.Viewdate)
                    : (dbFile.Watched, dbFile.WatchedUpdated);
                if (item.State != expectedState || item.FileState != MyListFileState.Normal || item.Viewdate is not null != syncedWatched)
                    toUpdate.Add(new UpdateMyListArgs(true, expectedState, syncedWatched, syncedWatchedDateTime, item.Id, item.Fid));
                if (dbFile.Watched != syncedWatched)
                {
                    var file = _context.AniDbFiles.Find(dbFile.Id);
                    if (file is not null)
                    {
                        file.Watched = syncedWatched;
                        file.WatchedUpdated = null;
                    }
                    else
                    {
                        var episode = _context.AniDbEpisodes.GetEpisodeByGenericFileId(_context.AniDbGenericFiles, dbFile.Id);
                        if (episode is not null)
                        {
                            episode.Watched = syncedWatched;
                            episode.WatchedUpdated = null;
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
                    toProcess.Add(new ProcessArgs(item.Fid, IdType.FileId));
                else if (item.State != _options.MyList.AbsentFileState || item.FileState != MyListFileState.Normal)
                    toUpdate.Add(new UpdateMyListArgs(true, _options.MyList.AbsentFileState, item.Viewdate is not null, item.Viewdate, item.Id, item.Fid));
            }
    }

    private HashSet<int> BulkMarkAbsent(List<MyListItem> myListItems)
    {
        var epIdsWithManualLinks = _context.EpisodesWithManualLinks.Select(e => e.Id).ToHashSet();
        var fileIdsWithLocal = _context.FilesWithLocal.Select(f => f.Id).ToHashSet();

        // What if local file is missing an episode relation to another anime?
        //     If file id is not checked, the file could be marked absent.
        // Checking file ids will cover all regular files, but not manual links, so check for episodes with manual links too
        var animeIdsToMarkAbsent = (from item in myListItems
            from aid in item.Aids
            group item by aid
            into animeGroup
            where !fileIdsWithLocal.Intersect(animeGroup.Select(i => i.Fid)).Any() &&
                  !epIdsWithManualLinks.Intersect(animeGroup.SelectMany(i => i.Eids)).Any() &&
                  animeGroup.Any(i => i.State != _options.MyList.AbsentFileState || i.FileState != MyListFileState.Normal)
            select animeGroup.Key).ToHashSet();

        _commandService.DispatchRange(animeIdsToMarkAbsent.Select(aid =>
            new UpdateMyListArgs(Aid: aid, EpNo: "0", Edit: true, MyListState: _options.MyList.AbsentFileState)));

        return animeIdsToMarkAbsent;
    }

    private void SyncMyListEntries(List<AniDbMyListEntry> myListEntries)
    {
        var eMyListEntries = _context.AniDbMyListEntries.ToList();

        var toDelete = eMyListEntries.ExceptBy(myListEntries.Select(e => e.Id), e => e.Id).ToList();
        _context.AniDbMyListEntries.RemoveRange(toDelete);

        foreach (var entry in myListEntries)
            if (_context.AniDbMyListEntries.FirstOrDefault(e => e.Id == entry.Id) is var ee && ee is null)
                _context.AniDbMyListEntries.Add(entry);
            else
                _context.Entry(ee).CurrentValues.SetValues(entry);

        _context.SaveChanges();
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
                File.Create(FilePaths.MyListPath).Dispose();
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
