using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Shizou.Data.CommandInputArgs;
using Shizou.Data.Database;
using Shizou.Server.Services;

namespace Shizou.Server.Commands;

public class AvDumpCommand : Command<AvDumpArgs>
{
    private readonly AvDumpService _avDumpService;
    private readonly IShizouContext _context;
    private readonly ILogger<AvDumpCommand> _logger;

    public AvDumpCommand(AvDumpService avDumpService, IShizouContext context, ILogger<AvDumpCommand> logger)
    {
        _avDumpService = avDumpService;
        _context = context;
        _logger = logger;
    }

    protected override async Task ProcessInnerAsync()
    {
        var localFile = _context.LocalFiles.Include(lf => lf.ImportFolder).FirstOrDefault(lf => lf.Id == CommandArgs.LocalFileId);
        if (localFile is null)
        {
            _logger.LogWarning("Local file for id: {LocalFileId} does not exist", CommandArgs.LocalFileId);
            Completed = true;
            return;
        }

        _logger.LogInformation("Starting AVDump for local file id: {LocalFileId}", CommandArgs.LocalFileId);
        await _avDumpService.RunAvDumpAsync(localFile).ConfigureAwait(false);

        Completed = true;
    }
}
