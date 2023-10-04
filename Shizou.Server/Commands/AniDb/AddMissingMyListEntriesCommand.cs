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
        var filesMissingMyListId = (from ws in _context.FileWatchedStates
            where ws.MyListId == null && ws.AniDbFile.LocalFile != null
            select new { Fid = ws.AniDbFileId, ws.Watched, ws.WatchedUpdated }).ToList();

        var genericFilesMissingMyListId = (from ws in _context.EpisodeWatchedStates
            where ws.MyListId == null && ws.AniDbEpisode.ManualLinkLocalFiles.Any()
            join gf in _context.AniDbGenericFiles on ws.AniDbEpisodeId equals gf.AniDbEpisodeId
            select new { Fid = gf.Id, ws.Watched, ws.WatchedUpdated }).ToList();

        var episodesWithMissingGenericFile = (from ep in _context.AniDbEpisodes.WithManualLinks()
            join ws in _context.EpisodeWatchedStates
                on ep.Id equals ws.AniDbEpisodeId
            where !_context.AniDbGenericFiles.Any(gf => gf.AniDbEpisodeId == ep.Id)
            select new { ep.AniDbAnimeId, ep.EpisodeType, ep.Number, ws.Watched, ws.WatchedUpdated }).ToList();

        _commandService.DispatchRange(filesMissingMyListId.Union(genericFilesMissingMyListId).Select(f =>
            new UpdateMyListArgs(false, _options.AniDb.MyList.PresentFileState, f.Watched, f.WatchedUpdated, Fid: f.Fid)));
        _commandService.DispatchRange(episodesWithMissingGenericFile.Select(e =>
            new UpdateMyListArgs(false, _options.AniDb.MyList.PresentFileState, e.Watched, e.WatchedUpdated,
                Aid: e.AniDbAnimeId, EpNo: EpisodeTypeExtensions.ToEpString(e.EpisodeType, e.Number))));

        Completed = true;
        return Task.CompletedTask;
    }
}
