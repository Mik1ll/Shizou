using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Shizou.Data.CommandInputArgs;
using Shizou.Data.Database;
using Shizou.Server.Options;
using Shizou.Server.Services;

namespace Shizou.Server.Commands.AniDb;

public class AddMissingMyListEntriesCommand : Command<AddMissingMyListEntriesArgs>
{
    private readonly CommandService _commandService;
    private readonly ILogger<AddMissingMyListEntriesCommand> _logger;
    private readonly IShizouContext _context;
    private readonly ShizouOptions _options;

    public AddMissingMyListEntriesCommand(
        IShizouContext context,
        CommandService commandService,
        IOptionsSnapshot<ShizouOptions> options,
        ILogger<AddMissingMyListEntriesCommand> logger)
    {
        _context = context;
        _commandService = commandService;
        _logger = logger;
        _options = options.Value;
    }

    protected override Task ProcessInnerAsync()
    {
        var filesMissingMyListId = (from ws in _context.FileWatchedStates
            where ws.MyListId == null && ws.AniDbFile.LocalFiles.Any()
            select new { Fid = ws.AniDbFileId, ws.Watched, ws.WatchedUpdated }).ToList();

        if (filesMissingMyListId.Count > 0)
            _logger.LogInformation("Found {NumFilesMissingMyListId} files with missing mylist entries, queueing mylist updates",
                filesMissingMyListId.Count);
        else
            _logger.LogInformation("No files are missing mylist entries");
        _commandService.Dispatch(filesMissingMyListId.Select(f =>
            new AddMyListArgs(f.Fid, _options.AniDb.MyList.PresentFileState, f.Watched, f.WatchedUpdated)));

        Completed = true;
        return Task.CompletedTask;
    }
}
