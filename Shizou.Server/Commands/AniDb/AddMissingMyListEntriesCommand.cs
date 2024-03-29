﻿using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Shizou.Data.CommandInputArgs;
using Shizou.Data.Database;
using Shizou.Data.Enums;
using Shizou.Server.Options;
using Shizou.Server.Services;

namespace Shizou.Server.Commands.AniDb;

public class AddMissingMyListEntriesCommand : Command<AddMissingMyListEntriesArgs>
{
    private readonly CommandService _commandService;
    private readonly ILogger<AddMissingMyListEntriesCommand> _logger;
    private readonly IShizouContext _context;
    private readonly ShizouOptions _options;

    public AddMissingMyListEntriesCommand(
        IShizouContext context,
        CommandService commandService,
        IOptionsSnapshot<ShizouOptions> options,
        ILogger<AddMissingMyListEntriesCommand> logger)
    {
        _context = context;
        _commandService = commandService;
        _logger = logger;
        _options = options.Value;
    }

    protected override Task ProcessInnerAsync()
    {
        var filesMissingMyListId = (from ws in _context.FileWatchedStates
            where ws.MyListId == null && ws.AniDbFile.LocalFile != null
            select new { Fid = ws.AniDbFileId, ws.Watched, ws.WatchedUpdated }).ToList();

        var genericFilesMissingMyListId = (from ws in _context.EpisodeWatchedStates
            where ws.MyListId == null && ws.AniDbFileId != null && ws.AniDbEpisode.ManualLinkLocalFiles.Any()
            select new { Fid = ws.AniDbFileId!.Value, ws.Watched, ws.WatchedUpdated }).ToList();

        var episodesWithMissingGenericFile = (from ep in _context.AniDbEpisodes
            where ep.ManualLinkLocalFiles.Any() && ep.EpisodeWatchedState.AniDbFileId == null
            select new { ep.AniDbAnimeId, ep.EpisodeType, ep.Number, ep.EpisodeWatchedState.Watched, ep.EpisodeWatchedState.WatchedUpdated }).ToList();
        if (filesMissingMyListId.Count > 0 || genericFilesMissingMyListId.Count > 0)
            _logger.LogInformation(
                "Found {NumFilesMissingMyListId} regular files and {NumGenericFilesMissingMyListId} generic files with missing mylist entries, queueing mylist updates",
                filesMissingMyListId.Count, genericFilesMissingMyListId.Count);
        else
            _logger.LogInformation("No files are missing mylist entries");
        _commandService.DispatchRange(filesMissingMyListId.Union(genericFilesMissingMyListId).Select(f =>
            new UpdateMyListArgs(false, _options.AniDb.MyList.PresentFileState, f.Watched, f.WatchedUpdated, Fid: f.Fid)));

        if (episodesWithMissingGenericFile.Count > 0)
            _logger.LogInformation("Found {NumEpisodesMissingGenericFile} manually linked episodes with missing generic file, queueing mylist updates",
                episodesWithMissingGenericFile.Count);
        else
            _logger.LogInformation("No manually linked episodes are missing generic files");
        _commandService.DispatchRange(episodesWithMissingGenericFile.Select(e =>
            new UpdateMyListArgs(false, _options.AniDb.MyList.PresentFileState, e.Watched, e.WatchedUpdated,
                Aid: e.AniDbAnimeId, EpNo: e.EpisodeType.GetEpString(e.Number))));

        Completed = true;
        return Task.CompletedTask;
    }
}
