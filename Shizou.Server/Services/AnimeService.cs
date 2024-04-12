using System.Linq;
using Microsoft.EntityFrameworkCore;
using Shizou.Data.CommandInputArgs;
using Shizou.Data.Database;
using Shizou.Data.Enums;

namespace Shizou.Server.Services;

public class AnimeService
{
    private readonly IShizouContextFactory _contextFactory;
    private readonly CommandService _commandService;

    public AnimeService(IShizouContextFactory contextFactory, CommandService commandService
    )
    {
        _contextFactory = contextFactory;
        _commandService = commandService;
    }

    public void GetMissingAnime()
    {
        using var context = _contextFactory.CreateDbContext();
        var hangingEps = context.HangingEpisodeFileXrefs.AsNoTracking().ToList();
        var filesMissingRelations = (from f in context.AniDbFiles
            where !f.AniDbEpisodeFileXrefs.Any() && !context.HangingEpisodeFileXrefs.Any(x => x.AniDbNormalFileId == f.Id)
            select f.Id).ToList();

        _commandService.DispatchRange(hangingEps.Select(h => new GetAnimeByEpisodeIdArgs(h.AniDbEpisodeId)));
        _commandService.DispatchRange(filesMissingRelations.Select(f => new ProcessArgs(f, IdTypeLocalOrFile.FileId)));
    }
}
