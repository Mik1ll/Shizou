using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Shizou.Data;
using Shizou.Data.CommandInputArgs;
using Shizou.Data.Database;
using Shizou.Server.Services;

namespace Shizou.Server.Commands;

public class ExtractThumbnailCommand : Command<ExtractThumbnailArgs>
{
    private readonly ILogger<ExtractThumbnailCommand> _logger;
    private readonly IShizouContext _shizouContext;
    private readonly FfmpegService _ffmpegService;

    public ExtractThumbnailCommand(ILogger<ExtractThumbnailCommand> logger, IShizouContext shizouContext, FfmpegService ffmpegService)
    {
        _logger = logger;
        _shizouContext = shizouContext;
        _ffmpegService = ffmpegService;
    }

    protected override async Task ProcessInnerAsync()
    {
        var localFile = _shizouContext.LocalFiles.Include(lf => lf.ImportFolder).FirstOrDefault(lf => lf.Id == CommandArgs.LocalFileId);
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

        var path = Path.Combine(localFile.ImportFolder.Path, localFile.PathTail);
        var fileInfo = new FileInfo(path);
        if (!fileInfo.Exists)
        {
            _logger.LogWarning("Local file {LocalFileId} does not exist at \"{Path}\"", localFile.Id, path);
            Completed = true;
            return;
        }

        await _ffmpegService.ExtractThumbnailAsync(fileInfo, FilePaths.ExtraFileData.ThumbnailPath(localFile.Ed2k)).ConfigureAwait(false);
        Completed = true;
    }
}
