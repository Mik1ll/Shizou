using System;
using System.Linq;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Shizou.Data.CommandInputArgs;
using Shizou.Data.Database;
using Shizou.Server.Extensions.Query;
using Shizou.Server.Options;

namespace Shizou.Server.Services;

public class WatchStateService
{
    private readonly CommandService _commandService;
    private readonly IShizouContextFactory _contextFactory;
    private readonly ILogger<WatchStateService> _logger;
    private readonly IOptionsMonitor<ShizouOptions> _optionsMonitor;

    public WatchStateService(ILogger<WatchStateService> logger, IShizouContextFactory contextFactory, CommandService commandService,
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
            _logger.LogWarning("File Id {FileId} watched state not found, not marking", fileId);
            return false;
        }

        fileWatchedState.Watched = watched;
        fileWatchedState.WatchedUpdated = updatedTime;

        context.SaveChanges();
        var myListOptions = _optionsMonitor.CurrentValue.AniDb.MyList;
        var state = myListOptions.PresentFileState;
        if (fileWatchedState.MyListId is not null)
            _commandService.Dispatch(new UpdateMyListArgs(fileWatchedState.MyListId.Value, state, watched, updatedTime));
        else
            _commandService.Dispatch(new AddMyListArgs(fileId, state, watched, updatedTime));
        return true;
    }

    public bool MarkAnime(int animeId, bool watched)
    {
        var updatedTime = DateTime.UtcNow;
        using var context = _contextFactory.CreateDbContext();

        var filesWithLocal = (from file in context.AniDbFiles.ByAnimeId(animeId)
            where file.LocalFiles.Any()
            select file).ToList();

        foreach (var f in filesWithLocal)
            if (!MarkFile(f.Id, watched, updatedTime))
                return false;

        return true;
    }
}
