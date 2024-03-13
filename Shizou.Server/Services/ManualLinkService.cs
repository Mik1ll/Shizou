using System.IO;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Shizou.Data.CommandInputArgs;
using Shizou.Data.Database;
using Shizou.Data.Models;
using Shizou.Server.Options;

namespace Shizou.Server.Services;

public class ManualLinkService
{
    private readonly CommandService _commandService;
    private readonly IShizouContextFactory _contextFactory;
    private readonly IOptionsMonitor<ShizouOptions> _optionsMonitor;
    private readonly ILogger<ManualLinkService> _logger;

    public ManualLinkService(CommandService commandService, IShizouContextFactory contextFactory, IOptionsMonitor<ShizouOptions> optionsMonitor,
        ILogger<ManualLinkService> logger)
    {
        _commandService = commandService;
        _contextFactory = contextFactory;
        _optionsMonitor = optionsMonitor;
        _logger = logger;
    }

    public enum LinkResult
    {
        Yes,
        Maybe,
        No
    }

    public LinkResult LinkFile(LocalFile localFile, int aniDbEpisodeId)
    {
        var options = _optionsMonitor.CurrentValue.AniDb.MyList;
        using var context = _contextFactory.CreateDbContext();
        context.LocalFiles.Attach(localFile);
        var episode = context.AniDbEpisodes.Include(e => e.AniDbFiles).ThenInclude(f => f.FileWatchedState).FirstOrDefault(e => e.Id == aniDbEpisodeId);
        if (episode is null)
        {
            _logger.LogError("Episode with id {EpisodeId} is not present, cannot link", aniDbEpisodeId);
            return LinkResult.No;
        }

        var genericFile = episode.AniDbFiles.OfType<AniDbGenericFile>().FirstOrDefault();
        if (genericFile is null)
        {
            _logger.LogError("Generic file for episode {EpisodeId} is not present, attempting to link after mylist add", aniDbEpisodeId);
            _commandService.Dispatch(new UpdateMyListByEpisodeArgs(false, episode.AniDbAnimeId, episode.EpString, options.PresentFileState,
                ManualLinkToLocalFileId: localFile.Id));
            return LinkResult.Maybe;
        }

        localFile.AniDbFileId = genericFile.Id;
        context.SaveChanges();
        _logger.LogInformation("Local file id {LocalFileId} with name \"{LocalFileName}\" manually linked to episode id {EpisodeId}", localFile.Id,
            Path.GetFileName(localFile.PathTail), aniDbEpisodeId);
        var watchedState = genericFile.FileWatchedState;
        _commandService.Dispatch(watchedState.MyListId is not null
            ? new UpdateMyListArgs(true, options.PresentFileState, Lid: watchedState.MyListId)
            : new UpdateMyListArgs(false, options.PresentFileState, Fid: watchedState.AniDbFileId));
        return LinkResult.Yes;
    }
}
