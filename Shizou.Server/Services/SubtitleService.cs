using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Shizou.Data;
using Shizou.Data.Database;

namespace Shizou.Server.Services;

public class SubtitleService
{
    private readonly ILogger<SubtitleService> _logger;
    private readonly IShizouContextFactory _contextFactory;
    private readonly FfmpegService _ffmpegService;

    public SubtitleService(ILogger<SubtitleService> logger, IShizouContextFactory contextFactory, FfmpegService ffmpegService)
    {
        _logger = logger;
        _contextFactory = contextFactory;
        _ffmpegService = ffmpegService;
    }

    public static string[] ValidSubFormats { get; } = { "ass", "ssa", "srt", "webvtt", "subrip", "ttml", "text", "mov_text", "dvb_teletext" };
    public static string[] ValidFontFormats { get; } = { "ttf", "otf" };

    public async Task ExtractSubtitlesAsync(string ed2K)
    {
        using var context = _contextFactory.CreateDbContext();
        var localFile = context.LocalFiles.Include(e => e.ImportFolder).FirstOrDefault(e => e.Ed2k == ed2K);
        if (localFile is null)
            return;
        if (localFile.ImportFolder is null)
        {
            _logger.LogWarning("Tried to get local file {LocalFileId} with no import folder", localFile.Id);
            return;
        }

        var fullPath = Path.GetFullPath(Path.Combine(localFile.ImportFolder.Path, localFile.PathTail));
        var fileInfo = new FileInfo(fullPath);
        if (!fileInfo.Exists)
        {
            _logger.LogWarning("Local file path \"{FullPath}\" does not exist", fullPath);
            return;
        }

        var subsDir = FilePaths.ExtraFileData.SubsDir(ed2K);
        Directory.CreateDirectory(subsDir);

        var subStreams = await _ffmpegService.GetSubtitleStreamsAsync(fileInfo, ValidSubFormats,
            idx => Path.GetFileName(FilePaths.ExtraFileData.SubPath(ed2K, idx))).ConfigureAwait(false);
        if (subStreams.Count <= 0)
        {
            _logger.LogDebug("No valid streams for {LocalFileId}, skipping subtitle extraction", localFile.Id);
            return;
        }

        await _ffmpegService.ExtractSubtitlesAsync(fileInfo, subStreams, subsDir).ConfigureAwait(false);
    }

    // ReSharper disable once InconsistentNaming
    public async Task ExtractFontsAsync(string ed2k)
    {
        using var context = _contextFactory.CreateDbContext();
        var localFile = context.LocalFiles.Include(e => e.ImportFolder).FirstOrDefault(e => e.Ed2k == ed2k);
        if (localFile is null)
            return;
        if (localFile.ImportFolder is null)
        {
            _logger.LogWarning("Tried to get local file {LocalFileId} with no import folder", localFile.Id);
            return;
        }

        var fullPath = Path.GetFullPath(Path.Combine(localFile.ImportFolder.Path, localFile.PathTail));
        var fileInfo = new FileInfo(fullPath);
        if (!fileInfo.Exists)
        {
            _logger.LogWarning("Local file path \"{FullPath}\" does not exist", fullPath);
            return;
        }

        var fontsDir = FilePaths.ExtraFileData.FontsDir(ed2k);
        Directory.CreateDirectory(fontsDir);

        var fontStreams = await _ffmpegService.GetFontStreamsAsync(fileInfo, ValidFontFormats).ConfigureAwait(false);
        if (fontStreams.Count <= 0)
        {
            _logger.LogDebug("No valid streams for {LocalFileId}, skipping subtitle extraction", localFile.Id);
            return;
        }

        await _ffmpegService.ExtractFontsAsync(fileInfo, fontStreams, FilePaths.ExtraFileData.FontsDir(ed2k)).ConfigureAwait(false);
    }
}
