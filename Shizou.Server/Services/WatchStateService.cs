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
        if (context.FileWatchedStates.Find(fileId) is { } fileWatchedState)
        {
            fileWatchedState.Watched = watched;
            fileWatchedState.WatchedUpdated = updatedTime;
            context.SaveChanges();
        }
        else if ((from gf in context.AniDbGenericFiles
                     join ws in context.EpisodeWatchedStates
                         on gf.AniDbEpisodeId equals ws.Id
                     where gf.Id == fileId
                     select ws).FirstOrDefault() is { } episodeWatchedState)
        {
            episodeWatchedState.Watched = watched;
            episodeWatchedState.WatchedUpdated = updatedTime;
            context.SaveChanges();
        }
        else
        {
            _logger.LogWarning("File Id {FileId} not found, not marking", fileId);
            return false;
        }

        var eEntryId = context.AniDbMyListEntries.AsNoTracking().FirstOrDefault(e => e.FileId == fileId)?.Id;
        _commandService.Dispatch(eEntryId is not null
            ? new UpdateMyListArgs(true, Watched: watched, WatchedDate: updatedTime, Lid: eEntryId)
            : new UpdateMyListArgs(true, Watched: watched, WatchedDate: updatedTime, Fid: fileId));
        return true;
    }

    public bool MarkEpisode(int episodeId, bool watched)
    {
        using var context = _contextFactory.CreateDbContext();
        var updatedTime = DateTime.UtcNow;
        var (eEpisode, eWatchedState) = (from ep in context.AniDbEpisodes.Include(e => e.ManualLinkXrefs)
                join ws in context.EpisodeWatchedStates
                    on ep.Id equals ws.Id
                where ep.Id == episodeId
                select new { ep, ws }
            ).ToList().Select(x => (x.ep, x.ws))
            .FirstOrDefault();
        if (eEpisode is null)
        {
            _logger.LogWarning("Episode Id {EpisodeId} not found, not marking", episodeId);
            return false;
        }

        if (eWatchedState is null)
        {
            context.EpisodeWatchedStates.Add(new EpisodeWatchedState { Id = eEpisode.Id, Watched = watched, WatchedUpdated = updatedTime });
        }
        else
        {
            eWatchedState.Watched = watched;
            eWatchedState.WatchedUpdated = updatedTime;
        }

        context.SaveChanges();

        // Don't use generic episode mylist edit, because it edits all files not just generic
        var eFileId = context.AniDbGenericFiles.AsNoTracking().FirstOrDefault(gf => gf.AniDbEpisodeId == episodeId)?.Id;
        if (eFileId is not null)
        {
            var eEntryId = context.AniDbMyListEntries.AsNoTracking().FirstOrDefault(e => e.FileId == eFileId)?.Id;
            _commandService.Dispatch(eEntryId is not null
                ? new UpdateMyListArgs(true, Watched: watched, WatchedDate: updatedTime, Lid: eEntryId)
                : new UpdateMyListArgs(true, Watched: watched, WatchedDate: updatedTime, Fid: eFileId));
        }
        else
        {
            var myListOptions = _optionsMonitor.CurrentValue.MyList;
            var state = eEpisode.ManualLinkXrefs.Any() ? myListOptions.PresentFileState : myListOptions.AbsentFileState;
            _commandService.Dispatch(new UpdateMyListArgs(false, state, watched, updatedTime, Aid: eEpisode.AniDbAnimeId,
                EpNo: EpisodeTypeExtensions.ToEpString(eEpisode.EpisodeType, eEpisode.Number)));
        }

        return true;
    }

    public bool MarkAnime(int animeId, bool watched)
    {
        using var context = _contextFactory.CreateDbContext();
        var updatedTime = DateTime.UtcNow;
        var eAnime = context.AniDbAnimes.Include(a => a.AniDbEpisodes).FirstOrDefault(a => a.Id == animeId);
        if (eAnime is null)
        {
            _logger.LogWarning("Anime Id {AnimeId} not found, not marking", animeId);
            return false;
        }

        var filesWithLocal = (from f in context.FileWatchedStates
            where context.LocalFiles.Any(lf => lf.Ed2k == f.Ed2k)
            join xref in context.AniDbEpisodeFileXrefs
                on f.Id equals xref.AniDbFileId
            join e in context.AniDbEpisodes
                on xref.AniDbEpisodeId equals e.Id
            where e.AniDbAnimeId == animeId
            select f).ToList();
        var epsWithmanualLinks = (from ep in context.EpisodesWithManualLinks
            join ws in context.EpisodeWatchedStates
                on ep.Id equals ws.Id
            where ep.AniDbAnimeId == animeId
            select ws).ToList();
        foreach (var f in filesWithLocal)
        {
            f.Watched = watched;
            f.WatchedUpdated = updatedTime;
        }

        foreach (var ws in epsWithmanualLinks)
        {
            ws.Watched = watched;
            ws.WatchedUpdated = updatedTime;
        }

        context.SaveChanges();
        _commandService.Dispatch(new UpdateMyListArgs(true, Watched: watched, WatchedDate: updatedTime, Aid: animeId, EpNo: "0"));

        return true;
    }
}