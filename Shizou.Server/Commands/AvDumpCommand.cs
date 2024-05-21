using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Shizou.Data.CommandInputArgs;
using Shizou.Data.Database;
using Shizou.Server.Options;
using Shizou.Server.Services;

namespace Shizou.Server.Commands;

public class AvDumpCommand : Command<AvDumpArgs>
{
    private readonly AvDumpService _avDumpService;
    private readonly IShizouContext _context;
    private readonly ILogger<AvDumpCommand> _logger;
    private readonly AniDbOptions _aniDbOptions;

    public AvDumpCommand(AvDumpService avDumpService, IShizouContext context, ILogger<AvDumpCommand> logger, IOptionsSnapshot<ShizouOptions> optionsSnapshot)
    {
        _avDumpService = avDumpService;
        _context = context;
        _logger = logger;
        _aniDbOptions = optionsSnapshot.Value.AniDb;
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

        if (string.IsNullOrWhiteSpace(_aniDbOptions.Username))
            throw new ArgumentException("Cannot AVDump: AniDB username is not set");

        if (string.IsNullOrEmpty(_aniDbOptions.AvDump.UdpKey))
        {
            _logger.LogError("Cannot AVDump: UDP key is not set, please set a key in the account section of AniDB and update your settings");
            Completed = true;
            return;
        }

        _logger.LogInformation("Starting AVDump for local file id: {LocalFileId}", CommandArgs.LocalFileId);
        await _avDumpService.AvDumpFileAsync(localFile, _aniDbOptions.Username, _aniDbOptions.AvDump.UdpKey, _aniDbOptions.AvDump.UdpClientPort)
            .ConfigureAwait(false);

        Completed = true;
    }
}
