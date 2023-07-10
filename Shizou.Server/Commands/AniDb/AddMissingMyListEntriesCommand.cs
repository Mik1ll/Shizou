using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Shizou.Data.Database;
using Shizou.Data.Enums;
using Shizou.Server.Options;
using Shizou.Server.Services;

namespace Shizou.Server.Commands.AniDb;

public sealed record AddMissingMyListEntriesArgs() : CommandArgs($"{nameof(AddMissingMyListEntriesCommand)}");

[Command(CommandType.AddMissingMyListEntries, CommandPriority.Low, QueueType.AniDbUdp)]
public class AddMissingMyListEntriesCommand : BaseCommand<AddMissingMyListEntriesArgs>
{
    private readonly ShizouContext _context;
    private readonly CommandService _commandService;
    private readonly ShizouOptions _options;

    public AddMissingMyListEntriesCommand(
        ShizouContext context,
        CommandService commandService,
        IOptionsSnapshot<ShizouOptions> options
    )
    {
        _context = context;
        _commandService = commandService;
        _options = options.Value;
    }

    protected override Task ProcessInner()
    {
        var missingFileIds = (from f in _context.AniDbFiles
            where _context.LocalFiles.Any(lf => f.Ed2K == lf.Ed2K) && !_context.AniDbMyListEntries.Any(e => e.FileId == f.Id)
            select new { f.Id, f.Watched, f.WatchedUpdated }).ToList();

        var missingEpisodes = (from e in _context.AniDbEpisodes.Include(e => e.ManualLinkXrefs)
            where e.ManualLinkXrefs.Any() &&
                  _context.AniDbGenericFiles.Any(gf =>
                      gf.AniDbEpisodeId == e.Id && _context.AniDbMyListEntries.Any(mle => mle.FileId == gf.Id))
            select new { e.Id, e.EpisodeType, e.Number, e.AniDbAnimeId, e.Watched, e.WatchedUpdated }).ToList();

        _commandService.DispatchRange(missingFileIds.Select(f =>
            new UpdateMyListArgs(false, _options.MyList.PresentFileState, f.Watched, f.WatchedUpdated, Fid: f.Id)));
        _commandService.DispatchRange(missingEpisodes.Select(e =>
            new UpdateMyListArgs(false, _options.MyList.PresentFileState, e.Watched, e.WatchedUpdated,
                Aid: e.AniDbAnimeId, EpNo: EpisodeTypeExtensions.ToEpString(e.Number, e.EpisodeType))));
        Completed = true;
        return Task.CompletedTask;
    }
}
