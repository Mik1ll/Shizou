using System;
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

    public void Mark(int fileId, bool watched)
    {
        using var context = _contextFactory.CreateDbContext();
        var updatedTime = DateTime.UtcNow;
        int? eitherId;
        if (context.AniDbFiles.Find(fileId) is { } eFile)
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
            return;
        }
        var eEntryId = context.AniDbMyListEntries.AsNoTracking().FirstOrDefault(e => e.FileId == eitherId.Value)?.Id;
        _commandService.Dispatch(eEntryId is not null
            ? new UpdateMyListArgs(true, Watched: watched, WatchedDate: updatedTime, Lid: eEntryId)
            : new UpdateMyListArgs(true, Watched: watched, WatchedDate: updatedTime, Fid: eitherId));
    }

    public void MarkEpisode(int episodeId, bool watched)
    {
        using var context = _contextFactory.CreateDbContext();
        var updatedTime = DateTime.UtcNow;
        var eEpisode = context.AniDbEpisodes
            .Include(e => e.ManualLinkXrefs)
            .FirstOrDefault(ep => ep.Id == episodeId);
        if (eEpisode is null)
        {
            _logger.LogWarning("Episode Id {EpisodeId} not found, not marking", episodeId);
            return;
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
    }
}
