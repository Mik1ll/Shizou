using System;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Shizou.Data.Database;
using Shizou.Data.Enums;
using Shizou.Server.Commands.AniDb;
using Shizou.Server.Extensions.Query;
using Shizou.Server.Options;

namespace Shizou.Server.Services;

public class WatchStateService
{
    private readonly CommandService _commandService;
    private readonly IDbContextFactory<ShizouContext> _contextFactory;
    private readonly ILogger<WatchStateService> _logger;
    private readonly IOptionsMonitor<ShizouOptions> _optionsMonitor;

    public WatchStateService(ILogger<WatchStateService> logger, IDbContextFactory<ShizouContext> contextFactory, CommandService commandService,
        IOptionsMonitor<ShizouOptions> optionsMonitor)
    {
        _logger = logger;
        _contextFactory = contextFactory;
        _commandService = commandService;
        _optionsMonitor = optionsMonitor;
    }

    public bool MarkFile(int fileId, bool watched, DateTime? updatedTime = null)
    {
        updatedTime ??= DateTime.UtcNow;
        using var context = _contextFactory.CreateDbContext();
        if (context.FileWatchedStates.Find(fileId) is not { } fileWatchedState)
        {
            if (context.EpisodeWatchedStates.Where(ws => ws.AniDbFileId == fileId).Select(f => (int?)f.AniDbEpisodeId).FirstOrDefault() is { } episodeId)
                return MarkEpisode(episodeId, watched);
            _logger.LogWarning("File Id {FileId} watched state not found, not marking", fileId);
            return false;
        }

        fileWatchedState.Watched = watched;
        fileWatchedState.WatchedUpdated = updatedTime;

        context.SaveChanges();
        var myListOptions = _optionsMonitor.CurrentValue.AniDb.MyList;
        var state = myListOptions.PresentFileState;
        _commandService.Dispatch(fileWatchedState.MyListId is not null
            ? new UpdateMyListArgs(true, state, watched, updatedTime, fileWatchedState.MyListId)
            : new UpdateMyListArgs(false, state, watched, updatedTime, Fid: fileId));
        return true;
    }

    public bool MarkEpisode(int episodeId, bool watched, DateTime? updatedTime = null)
    {
        updatedTime ??= DateTime.UtcNow;
        using var context = _contextFactory.CreateDbContext();
        if (context.EpisodeWatchedStates.Where(ews => ews.AniDbEpisodeId == episodeId)
                .Select(ews => new
                {
                    WatchedState = ews,
                    Episode = new
                    {
                        ews.AniDbEpisode.AniDbAnimeId, ews.AniDbEpisode.EpisodeType, ews.AniDbEpisode.Number,
                        HasManualLinks = ews.AniDbEpisode.ManualLinkLocalFiles.Any()
                    }
                }).FirstOrDefault() is not { WatchedState: { } episodeWatchedState, Episode: { } episode })
        {
            _logger.LogWarning("Episode Id {EpisodeId} watched state/episode not found, not marking", episodeId);
            return false;
        }

        episodeWatchedState.Watched = watched;
        episodeWatchedState.WatchedUpdated = updatedTime;

        context.SaveChanges();


        var myListOptions = _optionsMonitor.CurrentValue.AniDb.MyList;
        var state = episode.HasManualLinks ? myListOptions.PresentFileState : myListOptions.AbsentFileState;

        // Don't use generic episode mylist edit, because it edits all files not just generic
        var fileId = context.EpisodeWatchedStates.Where(ws => ws.AniDbEpisodeId == episodeId && ws.AniDbFileId != null)
            .Select(ws => ws.AniDbFileId).FirstOrDefault();
        if (fileId is not null)
            _commandService.Dispatch(episodeWatchedState.MyListId is not null
                ? new UpdateMyListArgs(true, state, watched, updatedTime, episodeWatchedState.MyListId)
                : new UpdateMyListArgs(false, state, watched, updatedTime, Fid: fileId));
        else
            _commandService.Dispatch(new UpdateMyListArgs(false, state, watched, updatedTime, Aid: episode.AniDbAnimeId,
                EpNo: EpisodeTypeExtensions.ToEpString(episode.EpisodeType, episode.Number)));

        return true;
    }

    public bool MarkAnime(int animeId, bool watched)
    {
        var updatedTime = DateTime.UtcNow;
        using var context = _contextFactory.CreateDbContext();

        var filesWithLocal = (from file in context.AniDbFiles.ByAnimeId(animeId)
            where file.LocalFile != null
            select file).ToList();
        var epsWithManualLink = (from episode in context.AniDbEpisodes
            where episode.AniDbAnimeId == animeId && episode.ManualLinkLocalFiles.Any()
            select episode).ToList();

        foreach (var f in filesWithLocal)
            if (!MarkFile(f.Id, watched, updatedTime))
                return false;

        foreach (var ep in epsWithManualLink)
            if (!MarkEpisode(ep.Id, watched, updatedTime))
                return false;
        return true;
    }
}
