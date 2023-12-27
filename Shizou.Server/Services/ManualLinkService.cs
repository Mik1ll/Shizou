using System.Linq;
using Microsoft.EntityFrameworkCore;
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

    public ManualLinkService(CommandService commandService, IShizouContextFactory contextFactory, IOptionsMonitor<ShizouOptions> optionsMonitor)
    {
        _commandService = commandService;
        _contextFactory = contextFactory;
        _optionsMonitor = optionsMonitor;
    }

    public void LinkFile(LocalFile localFile, int aniDbEpisodeId)
    {
        var options = _optionsMonitor.CurrentValue.AniDb.MyList;
        using var context = _contextFactory.CreateDbContext();
        context.LocalFiles.Attach(localFile);
        localFile.ManualLinkEpisodeId = aniDbEpisodeId;
        var watchedState = context.EpisodeWatchedStates.Include(ws => ws.AniDbEpisode).First(ws => ws.AniDbEpisodeId == aniDbEpisodeId);
        if (watchedState.MyListId is not null)
            _commandService.Dispatch(new UpdateMyListArgs(true, options.PresentFileState, Lid: watchedState.MyListId));
        else if (watchedState.AniDbFileId is not null)
            _commandService.Dispatch(new UpdateMyListArgs(false, options.PresentFileState, Fid: watchedState.AniDbFileId));
        else
            _commandService.Dispatch(new UpdateMyListArgs(false, options.PresentFileState, Aid: watchedState.AniDbEpisode.AniDbAnimeId,
                EpNo: watchedState.AniDbEpisode.EpString));
        context.SaveChanges();
    }
}
