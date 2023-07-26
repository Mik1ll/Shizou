using System.Linq;
using System.Threading.Tasks;
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
        var missingFiles = (from f in _context.FilesWithLocal
            where !_context.AniDbMyListEntries.Any(e => e.FileId == f.Id)
            select new { f.Id, f.Watched, f.WatchedUpdatedLocally }).ToList();

        var missingGenericFiles = (from gf in _context.AniDbGenericFiles
            join e in _context.EpisodesWithManualLinks
                on gf.AniDbEpisodeId equals e.Id
            where !_context.AniDbMyListEntries.Any(mle => mle.FileId == gf.Id)
            select new { gf.Id, e.Watched, e.WatchedUpdatedLocally }).ToList();

        var episodesWithMissingGenericFile = (from e in _context.EpisodesWithManualLinks
            where !_context.AniDbGenericFiles.Any(gf => gf.AniDbEpisodeId == e.Id)
            select new
            {
                e.AniDbAnimeId, e.EpisodeType, e.Number, e.Watched,
                e.WatchedUpdatedLocally
            }).ToList();

        _commandService.DispatchRange(missingFiles.Union(missingGenericFiles).Select(f =>
            new UpdateMyListArgs(false, _options.MyList.PresentFileState, f.Watched, f.WatchedUpdatedLocally, Fid: f.Id)));
        _commandService.DispatchRange(episodesWithMissingGenericFile.Select(e =>
            new UpdateMyListArgs(false, _options.MyList.PresentFileState, e.Watched, e.WatchedUpdatedLocally,
                Aid: e.AniDbAnimeId, EpNo: EpisodeTypeExtensions.ToEpString(e.EpisodeType, e.Number))));

        Completed = true;
        return Task.CompletedTask;
    }
}
