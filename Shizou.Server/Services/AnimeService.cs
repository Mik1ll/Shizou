using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Shizou.Data.CommandInputArgs;
using Shizou.Data.Database;
using Shizou.Data.Enums;

namespace Shizou.Server.Services;

public class AnimeService
{
    private readonly IShizouContextFactory _contextFactory;
    private readonly CommandService _commandService;
    private readonly ILogger<AnimeService> _logger;

    public AnimeService(IShizouContextFactory contextFactory, CommandService commandService, ILogger<AnimeService> logger
    )
    {
        _contextFactory = contextFactory;
        _commandService = commandService;
        _logger = logger;
    }

    /// <summary>
    ///     Find files with missing/hanging relations and retrieve the missing data
    /// </summary>
    public void GetMissingEpisodesAndAnime()
    {
        using var context = _contextFactory.CreateDbContext();
        var hangingEps = context.HangingEpisodeFileXrefs.AsNoTracking().ToList();
        var filesMissingRelations = (from f in context.AniDbFiles
            where !f.AniDbEpisodeFileXrefs.Any() && !context.HangingEpisodeFileXrefs.Any(x => x.AniDbNormalFileId == f.Id)
            select f.Id).ToList();

        foreach (var hEp in hangingEps)
        {
            _logger.LogInformation("Found hanging file-episode relation: {FileId} - {EpisodeId}  Queueing Anime Get", hEp.AniDbNormalFileId,
                hEp.AniDbEpisodeId);
            _commandService.Dispatch(new GetAnimeByEpisodeIdArgs(hEp.AniDbEpisodeId));
        }

        foreach (var fileMissingRelations in filesMissingRelations)
        {
            _logger.LogInformation("Found file with no relations: {FileId}  Queuing File Process", fileMissingRelations);
            _commandService.Dispatch(new ProcessArgs(fileMissingRelations, IdTypeLocalOrFile.FileId));
        }
    }
}
