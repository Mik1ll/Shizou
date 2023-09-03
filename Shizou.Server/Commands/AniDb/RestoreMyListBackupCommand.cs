using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Shizou.Data;
using Shizou.Data.Database;
using Shizou.Data.Enums;
using Shizou.Data.Models;
using Shizou.Server.AniDbApi.Requests.Http;
using Shizou.Server.Options;
using Shizou.Server.Services;

namespace Shizou.Server.Commands.AniDb;

public record RestoreMyListBackupArgs(DateOnly Date) : CommandArgs($"{nameof(RestoreMyListBackupCommand)}");

[Command(CommandType.RestoreMyListBackup, CommandPriority.Low, QueueType.AniDbHttp)]
public class RestoreMyListBackupCommand : BaseCommand<RestoreMyListBackupArgs>
{
    private readonly ILogger<RestoreMyListBackupCommand> _logger;
    private readonly ShizouContext _context;
    private readonly HttpRequestFactory _httpRequestFactory;
    private readonly ShizouOptions _options;
    private readonly CommandService _commandService;

    public RestoreMyListBackupCommand(ILogger<RestoreMyListBackupCommand> logger,
        ShizouContext context,
        IOptionsSnapshot<ShizouOptions> options,
        HttpRequestFactory httpRequestFactory,
        CommandService commandService)
    {
        _logger = logger;
        _context = context;
        _httpRequestFactory = httpRequestFactory;
        _options = options.Value;
        _commandService = commandService;
    }

    protected override async Task ProcessInner()
    {
        var backupPath = Path.Combine(FilePaths.MyListBackupDir, CommandArgs.Date.ToString("yyyy-MM-dd") + ".xml");
        var backup = new XmlSerializer(typeof(MyListResult)).Deserialize(new XmlTextReader(backupPath)) as MyListResult;
        if (backup is null)
        {
            _logger.LogError("Failed to read mylist backup \"{BackupPath}\"", backupPath);
            Completed = true;
            return;
        }

        var request = _httpRequestFactory.MyListRequest();
        await request.Process();
        if (request.MyListResult is null)
        {
            _logger.LogError("Failed to get mylist from anidb");
            Completed = true;
            return;
        }

        var dbFiles = _context.FileWatchedStates.Select(ws => new { FileId = ws.Id, WatchedState = (IWatchedState)ws }).ToList()
            .Union((from gf in _context.AniDbGenericFiles
                join ws in _context.EpisodeWatchedStates
                    on gf.AniDbEpisodeId equals ws.Id
                select new { FileId = gf.Id, WatchedState = (IWatchedState)ws }).ToList()).ToDictionary(f => f.FileId);
        var dbFilesWithLocal = _context.FilesWithLocal.Select(f => f.Id)
            .Union(_context.GenericFilesWithManualLinks.Select(f => f.Id)).ToHashSet();
        List<UpdateMyListArgs> toUpdate = new();

        var backupMyList = backup.MyListItems.DistinctBy(i => i.Id).ToList();
        var myList = request.MyListResult.MyListItems.DistinctBy(i => i.Id).ToList();
        var joined = (from bitem in backupMyList
            join item in myList on bitem.Fid equals item.Fid into lj
            select new { bitem, item = lj.SingleOrDefault() }).ToList();
        foreach (var pair in joined)
        {
            var bitem = pair.bitem;
            var item = pair.item;
            var expectedState = _options.AniDb.MyList.AbsentFileState;
            var watched = false;
            DateTimeOffset? watchedDate = null;
            if ((bitem.Viewdate ?? item.Viewdate) is { } viewDate)
            {
                watched = true;
                watchedDate = DateTimeOffset.Parse(viewDate);
            }

            dbFiles.TryGetValue(bitem.Fid, out var dbFile);
            if (dbFile is not null)
            {
                // May not update some episode watch states locally because they don't have the associated generic file id
                var watchedState = dbFile.WatchedState;
                watchedState.Watched = watched;
                watchedState.WatchedUpdated = null;
                if (dbFilesWithLocal.Contains(dbFile.FileId))
                    expectedState = _options.AniDb.MyList.PresentFileState;
            }

            if (item is null)
                toUpdate.Add(new UpdateMyListArgs(false, expectedState, watched, watchedDate, Fid: bitem.Fid));
            else if (item.Viewdate is null && bitem.Viewdate is not null)
                toUpdate.Add(new UpdateMyListArgs(true, expectedState, watched, watchedDate, item.Id));
        }

        // ReSharper disable once MethodHasAsyncOverload
        _context.SaveChanges();

        _commandService.DispatchRange(toUpdate);
        Completed = true;
    }
}