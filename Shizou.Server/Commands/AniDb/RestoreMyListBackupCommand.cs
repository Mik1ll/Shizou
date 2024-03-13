using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Shizou.Data.CommandInputArgs;
using Shizou.Data.Database;
using Shizou.Server.AniDbApi.Requests.Http.Interfaces;
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

    protected override async Task ProcessInnerAsync() => throw new NotImplementedException();
    // _logger.LogInformation("Restoring mylist from backup on {BackupDate}", CommandArgs.Date);
    // var backupPath = CommandArgs switch
    // {
    //     { Date: { } date } => Path.Combine(FilePaths.MyListBackupDir, date.ToString("yyyy-MM-dd") + ".xml"),
    //     { Path: { } path } => path,
    //     _ => throw new ArgumentOutOfRangeException()
    // };
    // var backup = new XmlSerializer(typeof(MyListResult)).Deserialize(new XmlTextReader(backupPath)) as MyListResult;
    // if (backup is null)
    // {
    //     _logger.LogError("Failed to read mylist backup \"{BackupPath}\"", backupPath);
    //     Completed = true;
    //     return;
    // }
    //
    // _myListRequest.SetParameters();
    // await _myListRequest.ProcessAsync().ConfigureAwait(false);
    // if (_myListRequest.MyListResult is null)
    // {
    //     _logger.LogError("Failed to get mylist from anidb");
    //     Completed = true;
    //     return;
    // }
    //
    // var dbFiles = _context.FileWatchedStates.Select(ws => new { FileId = ws.AniDbFileId, WatchedState = (IWatchedState)ws }).ToList()
    //     .Union((from ws in _context.EpisodeWatchedStates
    //         where ws.AniDbFileId != null
    //         select new { FileId = ws.AniDbFileId!.Value, WatchedState = (IWatchedState)ws }).ToList()).ToDictionary(f => f.FileId);
    // var dbFilesWithLocal = _context.AniDbFiles.Where(f => f.LocalFile != null).Select(f => f.Id)
    //     .Union(_context.EpisodeWatchedStates.Where(ws => ws.AniDbFileId != null && ws.AniDbEpisode.ManualLinkLocalFiles.Any())
    //         .Select(ws => ws.AniDbFileId!.Value)).ToHashSet();
    // List<UpdateMyListArgs> toUpdate = new();
    //
    // var backupMyList = backup.MyListItems.DistinctBy(i => i.Id).ToList();
    // var myList = _myListRequest.MyListResult.MyListItems.DistinctBy(i => i.Id).ToList();
    // List<(MyListItemResult bitem, MyListItemResult? item)> joined = (from bitem in backupMyList
    //     join item in myList on bitem.Fid equals item.Fid into lj
    //     select (bitem, item: lj.SingleOrDefault())).ToList();
    // foreach (var pair in joined)
    // {
    //     var bitem = pair.bitem;
    //     var item = pair.item;
    //     var expectedState = _options.AniDb.MyList.AbsentFileState;
    //     var watched = false;
    //     DateTimeOffset? watchedDate = null;
    //     if ((bitem.Viewdate ?? item?.Viewdate) is { } viewDate)
    //     {
    //         watched = true;
    //         watchedDate = DateTimeOffset.Parse(viewDate);
    //     }
    //
    //     dbFiles.TryGetValue(bitem.Fid, out var dbFile);
    //     if (dbFile is not null)
    //     {
    //         // May not update some episode watch states locally because they don't have the associated generic file id
    //         var watchedState = dbFile.WatchedState;
    //         watchedState.Watched = watched;
    //         watchedState.WatchedUpdated = null;
    //         if (dbFilesWithLocal.Contains(dbFile.FileId))
    //             expectedState = _options.AniDb.MyList.PresentFileState;
    //     }
    //
    //     if (item is null)
    //         toUpdate.Add(new UpdateMyListArgs(false, expectedState, watched, watchedDate, Fid: bitem.Fid));
    //     else if (item.Viewdate is null && bitem.Viewdate is not null)
    //         toUpdate.Add(new UpdateMyListArgs(true, expectedState, watched, watchedDate, item.Id));
    // }
    //
    // _context.SaveChanges();
    //
    // _commandService.DispatchRange(toUpdate);
    // Completed = true;
}
