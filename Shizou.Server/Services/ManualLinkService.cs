﻿using System.IO;
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

    /// <summary>
    ///     Manually link a <see cref="LocalFile" /> to an <see cref="AniDbEpisode" /> by the episode ID. <br />
    ///     If a generic AniDB file for the episode does not exist in the database, the action cannot be performed immediately and a
    ///     <see cref="Shizou.Server.Commands.AniDb.UpdateMyListByEpisodeCommand" /> will be queued
    /// </summary>
    /// <param name="localFile"></param>
    /// <param name="aniDbEpisodeId"></param>
    /// <returns>A <see cref="LinkResult" /> describing whether the action was sucessful</returns>
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
        if (watchedState.MyListId is not null)
            _commandService.Dispatch(new UpdateMyListArgs(watchedState.MyListId.Value, options.PresentFileState, null, null));
        else
            _commandService.Dispatch(new AddMyListArgs(watchedState.AniDbFileId, options.PresentFileState, null, null));
        return LinkResult.Yes;
    }

    /// <summary>
    ///     Removes the manual link of a local file to an episode.
    /// </summary>
    /// <param name="localFileId"></param>
    public void UnlinkFile(int localFileId)
    {
        using var context = _contextFactory.CreateDbContext();
        var localFile = context.LocalFiles.Include(lf => lf.AniDbFile).FirstOrDefault(lf => lf.Id == localFileId);
        if (localFile is not null)
        {
            if (localFile.AniDbFile is AniDbGenericFile)
            {
                var anidbFileId = localFile.AniDbFileId;
                localFile.AniDbFileId = null;
                context.SaveChanges();
                _logger.LogInformation("Unlinked local file id {LocalFileId} from AniDB generic file id {AniDbFileId}", localFile.Id, anidbFileId);
            }
            else
            {
                _logger.LogWarning("Tried to unlink local file id {LocalFileId} that does not have a linked generic file", localFileId);
            }
        }
        else
        {
            _logger.LogWarning("Tried to unlink local file id {LocalFileId} that does not exist", localFileId);
        }
    }
}
