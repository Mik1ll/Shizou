using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Shizou.Data.CommandInputArgs;
using Shizou.Data.Database;
using Shizou.Server.AniDbApi.Requests.Http;
using Shizou.Server.AniDbApi.Requests.Http.Interfaces;
using Shizou.Server.AniDbApi.Requests.Http.SubElements;
using Shizou.Server.Options;
using Shizou.Server.Services;

namespace Shizou.Server.Commands.AniDb;

public class RestoreMyListBackupCommand : Command<RestoreMyListBackupArgs>
{
    private readonly CommandService _commandService;
    private readonly IShizouContext _context;
    private readonly ILogger<RestoreMyListBackupCommand> _logger;
    private readonly IMyListRequest _myListRequest;
    private readonly ShizouOptions _options;

    public RestoreMyListBackupCommand(ILogger<RestoreMyListBackupCommand> logger,
        IShizouContext context,
        IOptionsSnapshot<ShizouOptions> options,
        CommandService commandService,
        IMyListRequest myListRequest)
    {
        _logger = logger;
        _context = context;
        _options = options.Value;
        _commandService = commandService;
        _myListRequest = myListRequest;
    }

    protected override async Task ProcessInnerAsync()
    {
        _logger.LogInformation("Restoring mylist from backup \"{BackupPath}\"", CommandArgs.Path);

        MyListResult? backup;
        if (Path.GetExtension(CommandArgs.Path) == ".zip")
        {
            var fs = new FileStream(CommandArgs.Path, FileMode.Open);
            await using var _ = fs.ConfigureAwait(false);
            using var arch = new ZipArchive(fs, ZipArchiveMode.Read);
            backup = new XmlSerializer(typeof(MyListResult)).Deserialize(new XmlTextReader(arch.Entries.First().Open())) as MyListResult;
        }
        else
        {
            backup = new XmlSerializer(typeof(MyListResult)).Deserialize(new XmlTextReader(CommandArgs.Path)) as MyListResult;
        }

        if (backup is null)
        {
            _logger.LogError("Failed to read mylist backup \"{BackupPath}\"", CommandArgs.Path);
            Completed = true;
            return;
        }

        _myListRequest.SetParameters();
        await _myListRequest.ProcessAsync().ConfigureAwait(false);
        if (_myListRequest.MyListResult is null)
        {
            _logger.LogError("Failed to get mylist from anidb");
            Completed = true;
            return;
        }

        var dbWatchStates = _context.FileWatchedStates.ToDictionary(ws => ws.AniDbFileId);
        var dbFilesWithLocal = _context.AniDbFiles.Where(f => f.LocalFiles.Any()).Select(f => f.Id).ToHashSet();
        List<CommandArgs> toUpdate = [];

        var backupMyList = backup.MyListItems.DistinctBy(i => i.Id).ToList();
        var myList = _myListRequest.MyListResult.MyListItems.DistinctBy(i => i.Id).ToList();
        List<(MyListItemResult bitem, MyListItemResult? item)> joined = (from bitem in backupMyList
            join item in myList on bitem.Fid equals item.Fid into lj
            select (bitem, item: lj.SingleOrDefault())).ToList();
        foreach (var pair in joined)
        {
            var bitem = pair.bitem;
            var item = pair.item;
            var expectedState = _options.AniDb.MyList.AbsentFileState;
            var watched = false;
            DateTimeOffset? watchedDate = null;
            if ((bitem.Viewdate ?? item?.Viewdate) is { } viewDate)
            {
                watched = true;
                watchedDate = DateTimeOffset.Parse(viewDate);
            }

            dbWatchStates.TryGetValue(bitem.Fid, out var dbWatchState);
            if (dbWatchState is not null)
            {
                dbWatchState.Watched = watched;
                dbWatchState.WatchedUpdated = null;
                if (dbFilesWithLocal.Contains(dbWatchState.AniDbFileId))
                    expectedState = _options.AniDb.MyList.PresentFileState;
            }

            if (item is null)
                toUpdate.Add(new AddMyListArgs(bitem.Fid, expectedState, watched, watchedDate));
            else if (item.Viewdate is not null != watched || item.State != expectedState)
                toUpdate.Add(new UpdateMyListArgs(item.Id, expectedState, watched, watchedDate, item.Fid));
        }

        _context.SaveChanges();

        _commandService.Dispatch(toUpdate);
        Completed = true;
    }
}
