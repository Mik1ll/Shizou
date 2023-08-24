﻿using System;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Shizou.Data.Database;
using Shizou.Data.Enums;
using Shizou.Server.Commands.AniDb;
using Shizou.Server.Extensions;
using Shizou.Server.Options;

namespace Shizou.Server.Services;

public class WatchStateService
{
    private readonly ILogger<WatchStateService> _logger;
    private readonly IDbContextFactory<ShizouContext> _contextFactory;
    private readonly CommandService _commandService;
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
        int? eitherId;
        if (context.FileWatchedStates.Find(fileId) is { } eFile)
        {
            eitherId = eFile.Id;
            eFile.Watched = watched;
            eFile.WatchedUpdatedLocally = updatedTime;
            context.SaveChanges();
        }
        else if (context.AniDbEpisodes.GetEpisodeByGenericFileId(context.AniDbGenericFiles, fileId) is { } eGenericFile)
        {
            eitherId = eGenericFile.Id;
            eGenericFile.Watched = watched;
            eGenericFile.WatchedUpdatedLocally = updatedTime;
            context.SaveChanges();
        }
        else
        {
            _logger.LogWarning("File Id {FileId} not found, not marking", fileId);
            return false;
        }

        var eEntryId = context.AniDbMyListEntries.AsNoTracking().FirstOrDefault(e => e.FileId == eitherId.Value)?.Id;
        _commandService.Dispatch(eEntryId is not null
            ? new UpdateMyListArgs(true, Watched: watched, WatchedDate: updatedTime, Lid: eEntryId)
            : new UpdateMyListArgs(true, Watched: watched, WatchedDate: updatedTime, Fid: eitherId));
        return true;
    }

    public bool MarkEpisode(int episodeId, bool watched)
    {
        using var context = _contextFactory.CreateDbContext();
        var updatedTime = DateTime.UtcNow;
        var eEpisode = context.AniDbEpisodes
            .Include(e => e.ManualLinkXrefs)
            .FirstOrDefault(ep => ep.Id == episodeId);
        if (eEpisode is null)
        {
            _logger.LogWarning("Episode Id {EpisodeId} not found, not marking", episodeId);
            return false;
        }

        eEpisode.Watched = watched;
        eEpisode.WatchedUpdatedLocally = updatedTime;
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
            where ep.AniDbAnimeId == animeId
            select ep).ToList();
        foreach (var f in filesWithLocal)
        {
            f.Watched = watched;
            f.WatchedUpdatedLocally = updatedTime;
        }

        foreach (var ep in epsWithmanualLinks)
        {
            ep.Watched = watched;
            ep.WatchedUpdatedLocally = updatedTime;
        }

        context.SaveChanges();
        _commandService.Dispatch(new UpdateMyListArgs(true, Watched: watched, WatchedDate: updatedTime, Aid: animeId, EpNo: "0"));

        return true;
    }
}