using Microsoft.Extensions.Logging;
using Shizou.Database;

namespace Shizou.Services;

public class WatchStateService
{
    private readonly ILogger<WatchStateService> _logger;
    private readonly ShizouContext _context;
    private readonly CommandService _commandService;

    public WatchStateService(ILogger<WatchStateService> logger, ShizouContext context, CommandService commandService)
    {
        _logger = logger;
        _context = context;
        _commandService = commandService;
    }
}
