using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Shizou.Common.Enums;
using Shizou.Data;
using Shizou.Data.Database;
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
    private readonly IServiceProvider _provider;
    private readonly ShizouContext _context;
    private readonly CommandService _commandService;
    private readonly ShizouOptions _options;

    public TimeSpan MyListRequestPeriod { get; } = TimeSpan.FromHours(24);


    public SyncMyListCommand(
        SyncMyListArgs commandArgs,
        ILogger<SyncMyListCommand> logger,
        ShizouContext context,
        CommandService commandService,
        IOptionsSnapshot<ShizouOptions> options,
        IServiceProvider provider
    ) : base(commandArgs)
    {
        _logger = logger;
        _provider = provider;
        _context = context;
        _commandService = commandService;
        _options = options.Value;
    }

    public override async Task Process()
    {
        var myListResult = await GetMyList();

        if (myListResult is null)
        {
            Completed = true;
            return;
        }

        SyncMyListEntries(myListResult);

        MarkAbsentFiles(myListResult);

        FindGenericFiles(myListResult);

        Completed = true;
    }

    private void FindGenericFiles(HttpMyListResult myListResult)
    {
        var epsWithoutGenericFile = _context.AniDbEpisodes.Where(e => !_context.AniDbGenericFiles
                .Select(f => f.AniDbEpisodeId).Contains(e.Id))
            .Select(a => a.Id).ToHashSet();
        var anidbFiles = _context.AniDbFiles.Select(f => f.Id).Union(_context.AniDbGenericFiles.Select(f => f.Id)).ToHashSet();
        var commands = myListResult.MyListItems.Where(item => epsWithoutGenericFile.Contains(item.Eid) && !anidbFiles.Contains(item.Fid))
            .Select(item => new ProcessArgs(item.Fid, IdType.FileId));
        _commandService.DispatchRange(commands);
    }

    private void MarkAbsentFiles(HttpMyListResult myListResult)
    {
        var animeIds = _context.AniDbAnimes.Select(a => a.Id).ToHashSet();
        var missingAnime = myListResult.MyListItems.Where(i => !animeIds.Contains(i.Aid) && i.State != _options.MyList.AbsentFileState)
            .Select(item => item.Aid).ToHashSet();
        _commandService.DispatchRange(missingAnime.Select(aid =>
            new UpdateMyListArgs(Aid: aid, EpNo: "0", Edit: true, MyListState: _options.MyList.AbsentFileState)));


        var filesWithoutLocal = _context.AniDbFiles
            .ExceptBy(_context.LocalFiles.Select(l => l.Ed2K), f => f.Ed2K)
            .Select(f => f.Id).ToHashSet();
        var filesWithoutManualLinks = _context.AniDbGenericFiles
            .Where(f => _context.AniDbEpisodes.Include(e => e.ManualLinkLocalFiles)
                .Where(e => e.Id == f.AniDbEpisodeId && e.ManualLinkLocalFiles.Count == 0).Any())
            .Select(f => f.Id).ToHashSet();
        var noLocalFiles = myListResult.MyListItems.Where(item =>
            !missingAnime.Contains(item.Aid) &&
            (filesWithoutLocal.Contains(item.Fid) || (filesWithoutManualLinks.Contains(item.Fid) &&
                                                      item.State != _options.MyList.AbsentFileState)));
        _commandService.DispatchRange(noLocalFiles.Select(item =>
            new UpdateMyListArgs(item.Id, Edit: true, MyListState: _options.MyList.AbsentFileState)));
    }

    private void SyncMyListEntries(HttpMyListResult myListResult)
    {
        var remoteItems = myListResult.MyListItems;
        var localEntries = _context.AniDbMyListEntries.ToList();
        // Delete local entries that don't exist on anidb, use exceptby since they are not same objects
        var toDelete = localEntries.ExceptBy(remoteItems.Select(e => e.Id), e => e.Id).ToList();
        _context.AniDbMyListEntries.RemoveRange(toDelete);
        // Add new anidb entries that don't exist in local db
        var toAdd = remoteItems.ExceptBy(localEntries.Select(e => e.Id), e => e.Id).ToList();
        var fileIds = _context.AniDbFiles.Select(f => f.Id).ToHashSet();
        var genericFileIds = _context.AniDbGenericFiles.Select(e => e.Id).ToHashSet();
        foreach (var item in toAdd)
        {
            var newEntry = ItemToAniDbMyListEntry(item);
            _context.AniDbMyListEntries.Add(newEntry);
            if (fileIds.Contains(item.Fid))
            {
                var relatedFile = _context.AniDbFiles.Include(f => f.MyListEntry).First(f => f.Id == item.Fid);
                relatedFile.MyListEntry = newEntry;
            }
            else if (genericFileIds.Contains(item.Fid))
            {
                var relatedGenericFile = _context.AniDbGenericFiles.Include(f => f.MyListEntry).First(f => f.Id == item.Fid);
                relatedGenericFile.MyListEntry = newEntry;
            }
        }
        // Replace changed entries
        var toUpdate = myListResult.MyListItems.Except(toAdd);
        foreach (var myListItem in toUpdate)
        {
            var existingEntry = localEntries.FirstOrDefault(e => e.Id == myListItem.Id);
            if (existingEntry is not null)
                _context.Entry(existingEntry).CurrentValues.SetValues(ItemToAniDbMyListEntry(myListItem));
        }
        _context.SaveChanges();
    }

    private static AniDbMyListEntry ItemToAniDbMyListEntry(MyListItem item)
    {
        return new AniDbMyListEntry
        {
            Id = item.Id,
            Watched = item.Viewdate is not null,
            WatchedDate = item.Viewdate is null ? null : DateTime.Parse(item.Viewdate).ToUniversalTime(),
            MyListState = item.State,
            MyListFileState = item.FileState,
            Updated = DateTime.SpecifyKind(DateTime.Parse(item.Updated), DateTimeKind.Utc)
        };
    }

    private async Task<HttpMyListResult?> GetMyList()
    {
        var requestable = true;
        var fileInfo = new FileInfo(FilePaths.MyListPath);
        if (fileInfo.Exists)
            requestable = DateTime.UtcNow - fileInfo.LastWriteTimeUtc > MyListRequestPeriod;
        if (!requestable)
        {
            _logger.LogWarning("Failed to get mylist: already requested in last {Hours} hours", MyListRequestPeriod.Hours);
            return null;
        }

        var request = new MyListRequest(_provider);
        await request.Process();
        if (request.MyListResult is null)
        {
            if (!File.Exists(FilePaths.MyListPath))
                File.Create(FilePaths.MyListPath).Dispose();
            File.SetLastWriteTimeUtc(FilePaths.MyListPath, DateTime.UtcNow);
            _logger.LogWarning("Failed to get mylist data from AniDb, retry in {Hours} hours", MyListRequestPeriod.Hours);
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
