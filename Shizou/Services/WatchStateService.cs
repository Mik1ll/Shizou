using Microsoft.Extensions.Logging;
using Shizou.Commands;
using Shizou.Database;

namespace Shizou.Services;

public class WatchStateService
{
    private readonly ILogger<WatchStateService> _logger;
    private readonly ShizouContext _context;
    private readonly CommandManager _commandManager;

    public WatchStateService(ILogger<WatchStateService> logger, ShizouContext context, CommandManager commandManager)
    {
        _logger = logger;
        _context = context;
        _commandManager = commandManager;
    }
}
