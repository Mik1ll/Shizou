using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Shizou.Data.CommandInputArgs;
using Shizou.Data.Database;
using Shizou.Server.Services;

namespace Shizou.Server.Commands;

public class ExtractExtraDataCommand : Command<ExtractExtraDataArgs>
{
    private readonly ILogger<ExtractExtraDataCommand> _logger;
    private readonly IShizouContext _context;
    private readonly FfmpegService _ffmpegService;

    public ExtractExtraDataCommand(ILogger<ExtractExtraDataCommand> logger, IShizouContext context, FfmpegService ffmpegService)
    {
        _logger = logger;
        _context = context;
        _ffmpegService = ffmpegService;
    }

    protected override async Task ProcessInnerAsync()
    {
        var localFile = _context.LocalFiles.Include(lf => lf.ImportFolder).FirstOrDefault(lf => lf.Id == CommandArgs.LocalFileId);
        if (localFile is null)
        {
            _logger.LogWarning("Local file {LocalFileId} not found in database", CommandArgs.LocalFileId);
            Completed = true;
            return;
        }

        if (localFile.ImportFolder is null)
        {
            _logger.LogWarning("Local file id {LocalFileId} has no import folder", CommandArgs.LocalFileId);
            Completed = true;
            return;
        }

        await _ffmpegService.ExtractThumbnailAsync(localFile).ConfigureAwait(false);
        await _ffmpegService.ExtractSubtitlesAsync(localFile).ConfigureAwait(false);
        Completed = true;
    }
}
