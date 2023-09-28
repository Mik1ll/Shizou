using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Shizou.Data.Database;
using Shizou.Data.Enums;
using Shizou.Server.Extensions.Query;
using Shizou.Server.Options;
using Shizou.Server.Services;

namespace Shizou.Server.Commands.AniDb;

public sealed record AddMissingMyListEntriesArgs() : CommandArgs($"{nameof(AddMissingMyListEntriesCommand)}");

[Command(CommandType.AddMissingMyListEntries, CommandPriority.Low, QueueType.AniDbUdp)]
public class AddMissingMyListEntriesCommand : Command<AddMissingMyListEntriesArgs>
{
    private readonly CommandService _commandService;
    private readonly ShizouContext _context;
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
        var missingFiles = (from ws in _context.FileWatchedStates
            where _context.LocalFiles.Any(lf => ws.Ed2k == lf.Ed2k) &&
                  ws.MyListId == null
            select new { ws.Id, ws.Watched, ws.WatchedUpdated }).ToList();

        var missingGenericFiles = (from gf in _context.AniDbGenericFiles
            join ws in _context.EpisodeWatchedStates on gf.AniDbEpisodeId equals ws.Id
            where _context.LocalFiles.Any(lf => lf.ManualLinkEpisodeId == gf.AniDbEpisodeId) &&
                  ws.MyListId == null
            select new { gf.Id, ws.Watched, ws.WatchedUpdated }).ToList();

        var episodesWithMissingGenericFile = (from ep in _context.AniDbEpisodesWithManualLinks()
            join ws in _context.EpisodeWatchedStates
                on ep.Id equals ws.Id
            where !_context.AniDbGenericFiles.Any(gf => gf.AniDbEpisodeId == ep.Id)
            select new { ep.AniDbAnimeId, ep.EpisodeType, ep.Number, ws.Watched, ws.WatchedUpdated }).ToList();

        _commandService.DispatchRange(missingFiles.Union(missingGenericFiles).Select(f =>
            new UpdateMyListArgs(false, _options.AniDb.MyList.PresentFileState, f.Watched, f.WatchedUpdated, Fid: f.Id)));
        _commandService.DispatchRange(episodesWithMissingGenericFile.Select(e =>
            new UpdateMyListArgs(false, _options.AniDb.MyList.PresentFileState, e.Watched, e.WatchedUpdated,
                Aid: e.AniDbAnimeId, EpNo: EpisodeTypeExtensions.ToEpString(e.EpisodeType, e.Number))));

        Completed = true;
        return Task.CompletedTask;
    }
}
