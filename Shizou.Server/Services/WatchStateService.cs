using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Shizou.Data.Database;

namespace Shizou.Server.Services;

public class WatchStateService
{
    private readonly ILogger<WatchStateService> _logger;
    private readonly IDbContextFactory<ShizouContext> _contextFactory;
    private readonly CommandService _commandService;

    public WatchStateService(ILogger<WatchStateService> logger, IDbContextFactory<ShizouContext> contextFactory, CommandService commandService)
    {
        _logger = logger;
        _contextFactory = contextFactory;
        _commandService = commandService;
    }
}
