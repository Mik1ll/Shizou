using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Shizou.AniDbApi.Requests.Http;
using Shizou.AniDbApi.Requests.Http.Results;
using Shizou.CommandProcessors;
using Shizou.Database;
using Shizou.Models;
using Shizou.Options;
using Shizou.Services;

namespace Shizou.Commands.AniDb;

public record SyncMyListArgs() : CommandArgs($"{nameof(SyncMyListCommand)}");

[Command(CommandType.SyncMyList, CommandPriority.Normal, QueueType.AniDbHttp)]
public class SyncMyListCommand : BaseCommand<SyncMyListArgs>
{
    private readonly IServiceProvider _provider;
    private readonly ShizouContext _context;
    private readonly CommandService _commandService;
    private readonly ShizouOptions _options;

    public TimeSpan MyListRequestPeriod { get; } = TimeSpan.FromHours(24);


    public SyncMyListCommand(IServiceProvider provider, SyncMyListArgs commandArgs) : base(provider, commandArgs)
    {
        _provider = provider;
        _context = _provider.GetRequiredService<ShizouContext>();
        _commandService = _provider.GetRequiredService<CommandService>();
        using var scope = _provider.CreateScope();
        _options = scope.ServiceProvider.GetRequiredService<IOptionsSnapshot<ShizouOptions>>().Value;
    }

    public override async Task Process()
    {
        var myList = await GetMyList();

        if (myList is null)
        {
            Completed = true;
            return;
        }

        SyncMyListEntries(myList);

        Completed = true;
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
        var genericFileIds = _context.AniDbEpisodes.Select(e => e.GenericFileId).ToHashSet();
        foreach (var item in toAdd)
        {
            var newEntry = new AniDbMyListEntry(item);
            _context.AniDbMyListEntries.Add(newEntry);
            if (fileIds.Contains(item.Fid))
            {
                var relatedFile = _context.AniDbFiles.Include(f => f.MyListEntry).First(f => f.Id == item.Fid);
                relatedFile.MyListEntry = newEntry;
            }
            else if (genericFileIds.Contains(item.Fid))
            {
                var relatedEpisode = _context.AniDbEpisodes.Include(e => e.GenericMyListEntry).First(e => e.GenericFileId == item.Fid);
                relatedEpisode.GenericMyListEntry = newEntry;
            }
        }
        // Replace changed entries
        var toUpdate = myListResult.MyListItems.Except(toAdd);
        foreach (var myListItem in toUpdate)
        {
            var existingEntry = localEntries.FirstOrDefault(e => e.Id == myListItem.Id);
            if (existingEntry is not null)
                _context.Entry(existingEntry).CurrentValues.SetValues(new AniDbMyListEntry(myListItem));
        }
        _context.SaveChanges();
    }

    private async Task<HttpMyListResult?> GetMyList()
    {
        var requestable = true;
        var fileInfo = new FileInfo(Constants.MyListPath);
        if (fileInfo.Exists)
            requestable = DateTime.UtcNow - fileInfo.LastWriteTimeUtc > MyListRequestPeriod;
        if (!requestable)
        {
            Logger.LogWarning("Failed to get mylist: already requested in last {hours} hours", MyListRequestPeriod.Hours);
            return null;
        }

        var request = new MyListRequest(_provider);
        await request.Process();
        if (request.MyListResult is null)
        {
            if (!File.Exists(Constants.MyListPath))
                File.Create(Constants.MyListPath).Dispose();
            File.SetLastWriteTimeUtc(Constants.MyListPath, DateTime.UtcNow);
            Logger.LogWarning("Failed to get mylist data from AniDb, retry in {hours} hours", MyListRequestPeriod.Hours);
        }
        else
        {
            Logger.LogDebug("Overwriting mylist file");
            Directory.CreateDirectory(Path.GetDirectoryName(Constants.MyListPath)!);
            await File.WriteAllTextAsync(Constants.MyListPath, request.ResponseText, Encoding.UTF8);
            var backupFilePath = Path.Combine(Constants.MyListBackupDir, DateTime.UtcNow.ToString("yyyy-MM-dd") + ".xml");
            Directory.CreateDirectory(Constants.MyListBackupDir);
            await File.WriteAllTextAsync(backupFilePath, request.ResponseText, Encoding.UTF8);
            Logger.LogInformation("HTTP Get mylist succeeded");
        }
        return request.MyListResult;
    }
}
