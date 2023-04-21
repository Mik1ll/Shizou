﻿using System;
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
        foreach (var myListEntry in myList.MyListItems)
        {
            var updated = DateTime.SpecifyKind(DateTime.Parse(myListEntry.Updated), DateTimeKind.Utc);
            DateTime? watched = myListEntry.Viewdate is null ? null : DateTime.Parse(myListEntry.Viewdate).ToUniversalTime();
            var dbMyListEntry = context.AniDbMyListEntries.Find(myListEntry.Id);
        }

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
