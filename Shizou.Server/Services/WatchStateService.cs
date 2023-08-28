using System;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Shizou.Data.Database;
using Shizou.Data.Enums;
using Shizou.Data.Models;
using Shizou.Server.Commands.AniDb;
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

    public bool MarkFile(int fileId, bool watched)
    {
        using var context = _contextFactory.CreateDbContext();
        var updatedTime = DateTime.UtcNow;
        if (context.AniDbFiles.Select(f => new { f.Id, f.Ed2k }).FirstOrDefault() is not { } file)
        {
            if (context.AniDbGenericFiles.Find(fileId) is { } genericFile)
                return MarkEpisode(genericFile.AniDbEpisodeId, watched);
            _logger.LogWarning("File Id {FileId} not found, not marking", fileId);
            return false;
        }

        var watchedState = context.FileWatchedStates.Find(fileId);
        if (watchedState is null)
        {
            context.FileWatchedStates.Add(new FileWatchedState { Id = fileId, Ed2k = file.Ed2k, Watched = watched, WatchedUpdated = updatedTime });
        }
        else
        {
            watchedState.Watched = watched;
            watchedState.WatchedUpdated = updatedTime;
        }

        context.SaveChanges();
        var myListOptions = _optionsMonitor.CurrentValue.MyList;
        var state = myListOptions.PresentFileState;
        _commandService.Dispatch(watchedState?.MyListId is not null
            ? new UpdateMyListArgs(true, state, watched, updatedTime, watchedState.MyListId)
            : new UpdateMyListArgs(false, state, watched, updatedTime, Fid: fileId));
        return true;
    }

    public bool MarkEpisode(int episodeId, bool watched)
    {
        using var context = _contextFactory.CreateDbContext();
        var updatedTime = DateTime.UtcNow;
        if (context.AniDbEpisodes.AsNoTracking().Include(ep => ep.ManualLinkXrefs)
                .FirstOrDefault(ep => ep.Id == episodeId) is not { } episode)
        {
            _logger.LogWarning("Episode Id {EpisodeId} not found, not marking", episodeId);
            return false;
        }

        var watchedState = context.EpisodeWatchedStates.Find(episodeId);

        if (watchedState is null)
        {
            context.EpisodeWatchedStates.Add(new EpisodeWatchedState { Id = episodeId, Watched = watched, WatchedUpdated = updatedTime });
        }
        else
        {
            watchedState.Watched = watched;
            watchedState.WatchedUpdated = updatedTime;
        }

        context.SaveChanges();


        var myListOptions = _optionsMonitor.CurrentValue.MyList;
        var state = episode.ManualLinkXrefs.Any() ? myListOptions.PresentFileState : myListOptions.AbsentFileState;
        
        // Don't use generic episode mylist edit, because it edits all files not just generic
        var fileId = context.AniDbGenericFiles.AsNoTracking().FirstOrDefault(gf => gf.AniDbEpisodeId == episodeId)?.Id;
        if (fileId is not null)
        {
            _commandService.Dispatch(watchedState?.MyListId is not null
                ? new UpdateMyListArgs(true, state, watched, updatedTime, watchedState.MyListId)
                : new UpdateMyListArgs(false, state, watched, updatedTime, Fid: fileId));
        }
        else
        {
            _commandService.Dispatch(new UpdateMyListArgs(false, state, watched, updatedTime, Aid: episode.AniDbAnimeId,
                EpNo: EpisodeTypeExtensions.ToEpString(episode.EpisodeType, episode.Number)));
        }

        return true;
    }

    public bool MarkAnime(int animeId, bool watched)
    {
        using var context = _contextFactory.CreateDbContext();
        var updatedTime = DateTime.UtcNow;

        var files = (from file in context.FilesWithLocal
            join ws in context.FileWatchedStates
                on file.Id equals ws.Id into ws
            join xref in context.AniDbEpisodeFileXrefs
                on file.Id equals xref.AniDbFileId
            join ep in context.AniDbEpisodes
                on xref.AniDbEpisodeId equals ep.Id
            where ep.AniDbAnimeId == animeId
            select new { file, ws }).ToList();
        var episodes = (from episode in context.EpisodesWithManualLinks
            join ws in context.EpisodeWatchedStates
                on episode.Id equals ws.Id into ws
            where episode.AniDbAnimeId == animeId
            select new { episode, ws }).ToList();

        foreach (var f in files)
        {
            var watchedState = f.ws.FirstOrDefault();
            if (watchedState is null)
            {
                context.FileWatchedStates.Add(new FileWatchedState { Id = f.file.Id, Ed2k = f.file.Ed2k, Watched = watched, WatchedUpdated = updatedTime });
            }
            else
            {
                watchedState.Watched = watched;
                watchedState.WatchedUpdated = updatedTime;
            }
        }

        foreach (var ep in episodes)
        {
            var watchedState = ep.ws.FirstOrDefault();
            if (watchedState is null)
            {
                context.EpisodeWatchedStates.Add(new EpisodeWatchedState { Id = ep.episode.Id, Watched = watched, WatchedUpdated = updatedTime });
            }
            else
            {
                watchedState.Watched = watched;
                watchedState.WatchedUpdated = updatedTime;
            }
        }

        context.SaveChanges();
        _commandService.Dispatch(new UpdateMyListArgs(true, Watched: watched, WatchedDate: updatedTime, Aid: animeId, EpNo: "0"));

        return true;
    }
}