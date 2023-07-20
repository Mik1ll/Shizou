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
        var animeIdsToMarkAbsent = BulkMarkAbsent(myListItems);

        var fileIdsWithLocal = _context.FilesWithLocal.Select(f => f.Id).ToHashSet();

        var itemsWithPresentFiles = (from item in myListItems
            where fileIdsWithLocal.Contains(item.Fid)
            select item).ToList();

        var genericFileIdsWithManualLinks = _context.GenericFilesWithManualLinks.Select(gf => gf.Id).ToHashSet();

        var itemsWithPresentManualLinks = (from item in myListItems
            where genericFileIdsWithManualLinks.Contains(item.Fid)
            select item).ToList();

        var presentItems = itemsWithPresentFiles.Union(itemsWithPresentManualLinks).ToList();
        var presentItemIds = presentItems.Select(i => i.Id).ToHashSet();

        var itemsToMarkPresent = (from item in presentItems
            where item.State != _options.MyList.PresentFileState || item.FileState != MyListFileState.Normal
            select item).ToList();

        var itemsToMarkAbsent = (from item in myListItems
            where (!presentItemIds.Contains(item.Id) &&
                   !animeIdsToMarkAbsent.Intersect(item.Aids).Any() &&
                   item.State != _options.MyList.AbsentFileState) || item.FileState != MyListFileState.Normal
            select item).ToList();

        IEnumerable<UpdateMyListArgs> NewUpdateMyListArgs(List<MyListItem> items, MyListState newState)
        {
            return items.Select(myListItem => new UpdateMyListArgs(Lid: myListItem.Id, Fid: myListItem.Fid, Edit: true, MyListState: newState,
                Watched: myListItem.Viewdate is not null, WatchedDate: myListItem.Viewdate?.UtcDateTime));
        }

        _commandService.DispatchRange(NewUpdateMyListArgs(itemsToMarkPresent, _options.MyList.PresentFileState));
        _commandService.DispatchRange(NewUpdateMyListArgs(itemsToMarkAbsent, _options.MyList.AbsentFileState));
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
        //return new XmlSerializer(typeof(MyListResult)).Deserialize(new XmlTextReader(FilePaths.MyListPath)) as MyListResult;
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
