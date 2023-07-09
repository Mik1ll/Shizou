using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Shizou.Data;
using Shizou.Data.Database;
using Shizou.Data.Enums;
using Shizou.Data.Models;
using Shizou.Server.AniDbApi.Requests.Http;
using Shizou.Server.AniDbApi.Requests.Http.Results;
using Shizou.Server.AniDbApi.Requests.Http.Results.SubElements;
using Shizou.Server.Options;
using Shizou.Server.Services;

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
        var myListEntries = myListResult.MyListItems.DistinctBy(i => i.Id).Select(ItemToAniDbMyListEntry).ToList();

        SyncMyListEntries(myListEntries);

        MarkAbsentFiles(myListResult.MyListItems);

        FindGenericFiles(myListResult.MyListItems);

        Completed = true;
    }

    private static AniDbMyListEntry ItemToAniDbMyListEntry(MyListItem item)
    {
        return new AniDbMyListEntry
        {
            Id = item.Id,
            FileId = item.Fid,
            Watched = item.Viewdate is not null,
            WatchedDate = item.Viewdate is null ? null : DateTimeOffset.Parse(item.Viewdate).UtcDateTime,
            MyListState = item.State,
            MyListFileState = item.FileState,
            Updated = DateTime.UtcNow
        };
    }

    private void FindGenericFiles(List<MyListItem> myListItems)
    {
        var epsWithoutGenericFile = (from e in _context.AniDbEpisodes
            join f in _context.AniDbGenericFiles
                on e.Id equals f.AniDbEpisodeId into ef
            where !ef.Any()
            select e.Id).Distinct();
        var anidbFileIds = _context.AniDbFiles.Select(f => f.Id).Union(_context.AniDbGenericFiles.Select(f => f.Id)).ToHashSet();

        // Only retrieve files for episodes we have locally
        var missingFileIds = (from item in myListItems
            where !anidbFileIds.Contains(item.Fid) && epsWithoutGenericFile.Contains(item.Eid)
            select item.Fid).Distinct();
        var commands = missingFileIds.Select(fid => new ProcessArgs(fid, IdType.FileId)).ToList();
        _commandService.DispatchRange(commands);
    }

    private void MarkAbsentFiles(List<MyListItem> myListItems)
    {
        var animeIds = _context.AniDbAnimes.Select(a => a.Id).ToHashSet();
        var animeToBeMarkedAbsent = myListItems.Where(i => !animeIds.Contains(i.Aid) && i.State != _options.MyList.AbsentFileState)
            .Select(item => item.Aid).ToHashSet();
        _commandService.DispatchRange(animeToBeMarkedAbsent.Select(aid =>
            new UpdateMyListArgs(Aid: aid, EpNo: "0", Edit: true, MyListState: _options.MyList.AbsentFileState)));


        var filesWithoutLocal = (from f in _context.AniDbFiles
            where !_context.LocalFiles.Any(lf => lf.Ed2K == f.Ed2K)
            select f.Id).ToHashSet();
        var genericFilesWithoutManualLinks = (from gf in _context.AniDbGenericFiles
            join e in _context.AniDbEpisodes.Include(x => x.ManualLinkLocalFiles)
                on gf.AniDbEpisodeId equals e.Id
            where !e.ManualLinkLocalFiles.Any()
            select gf.Id).ToHashSet();

        var itemsToBeMarkedAbsent = from item in myListItems
            where item.State != _options.MyList.AbsentFileState &&
                  filesWithoutLocal.Union(genericFilesWithoutManualLinks).Contains(item.Fid)
            group item by item.Id
            into itemGroup
            where !animeToBeMarkedAbsent.Intersect(itemGroup.Select(i => i.Aid)).Any()
            select itemGroup.First();

        _commandService.DispatchRange(itemsToBeMarkedAbsent.Select(item =>
            new UpdateMyListArgs(Lid: item.Id, Edit: true, MyListState: _options.MyList.AbsentFileState, Watched: item.Viewdate is not null,
                WatchedDate: item.Viewdate is null ? null : DateTimeOffset.Parse(item.Viewdate).UtcDateTime)));
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

    private async Task<HttpMyListResult?> GetMyList()
    {
        //return new XmlSerializer(typeof(HttpMyListResult)).Deserialize(new XmlTextReader(FilePaths.MyListPath)) as HttpMyListResult;
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
