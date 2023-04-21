using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Shizou.AniDbApi.Requests.Http;
using Shizou.AniDbApi.Requests.Http.Results;
using Shizou.CommandProcessors;
using Shizou.Database;
using Shizou.Models;

namespace Shizou.Commands.AniDb;

public record SyncMyListArgs() : CommandArgs($"{nameof(SyncMyListCommand)}");

[Command(CommandType.SyncMyList, CommandPriority.Normal, QueueType.AniDbHttp)]
public class SyncMyListCommand : BaseCommand<SyncMyListArgs>
{
    private readonly IServiceProvider _provider;

    public TimeSpan MyListRequestPeriod { get; } = TimeSpan.FromHours(24);


    public SyncMyListCommand(IServiceProvider provider, SyncMyListArgs commandArgs) : base(provider, commandArgs)
    {
        _provider = provider;
    }

    public override async Task Process()
    {
        var myList = await GetMyList();

        if (myList is null)
        {
            Completed = true;
            return;
        }

        var context = _provider.GetRequiredService<ShizouContext>();
        var localEntries = context.AniDbMyListEntries.ToList();
        // Delete local entries that don't exist on anidb
        var toDelete = localEntries.Where(e => !myList.MyListItems.Select(i => i.Id).Contains(e.Id));
        context.AniDbMyListEntries.RemoveRange(toDelete);
        // Add new anidb entries that don't exist in local db
        var toAdd = myList.MyListItems.Where(i => !localEntries.Select(e => e.Id).Contains(i.Id)).ToList();
        foreach (var myListItem in toAdd)
        {
            var relatedFile = context.AniDbFiles.Include(f => f.MyListEntry).FirstOrDefault(f => f.Id == myListItem.Fid);
            if (relatedFile is not null)
            {
                relatedFile.MyListEntry = new AniDbMyListEntry(myListItem);
            }
            else
            {
                var relatedEpisode = context.AniDbEpisodes.Include(e => e.GenericMyListEntry).FirstOrDefault(e => e.GenericFileId == myListItem.Fid);
                if (relatedEpisode is not null)
                    relatedEpisode.GenericMyListEntry = new AniDbMyListEntry(myListItem);
            }
        }
        // Replace changed entries
        var toUpdate = myList.MyListItems.Except(toAdd);
        foreach (var myListItem in toUpdate)
        {
            var existingEntry = localEntries.FirstOrDefault(e => e.Id == myListItem.Id);
            if (existingEntry is not null)
                context.Entry(existingEntry).CurrentValues.SetValues(new AniDbMyListEntry(myListItem));
        }
        context.SaveChanges();

        Completed = true;
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
            await File.WriteAllTextAsync(Constants.MyListPath, request.ResponseText, Encoding.UTF8);
            var backupFilePath = Path.Combine(Constants.MyListBackupDir, DateTime.UtcNow.ToString("yyyy-MM-dd") + ".xml");
            await File.WriteAllTextAsync(backupFilePath, request.ResponseText, Encoding.UTF8);
            Logger.LogInformation("HTTP Get mylist succeeded");
        }
        return request.MyListResult;
    }
}
