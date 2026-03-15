using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Shizou.Data;
using Shizou.Data.Database;
using Shizou.Server.RHash;

namespace Shizou.Server.Services;

public class SubtitleService
{
    private readonly ILogger<SubtitleService> _logger;
    private readonly IShizouContextFactory _contextFactory;
    private readonly FfmpegService _ffmpegService;
    private readonly HashService _hashService;

    public SubtitleService(ILogger<SubtitleService> logger, IShizouContextFactory contextFactory, FfmpegService ffmpegService, HashService hashService)
    {
        _logger = logger;
        _contextFactory = contextFactory;
        _ffmpegService = ffmpegService;
        _hashService = hashService;
    }

    public static string[] ValidSubFormats { get; } = { "ass", "ssa", "srt", "webvtt", "subrip", "ttml", "text", "mov_text", "dvb_teletext" };
    public static string[] ValidFontFormats { get; } = { "ttf", "otf" };

    public async Task<string?> GetAttachmentPathAsync(string ed2K, string fileName)
    {
        using var context = _contextFactory.CreateDbContext();
        var attachment = await context.LocalFileAttachments
                                      .Include(a => a.LocalFile)
                                      .FirstOrDefaultAsync(a => a.LocalFile.Ed2k == ed2K && a.Filename == fileName)
                                      .ConfigureAwait(false);

        return attachment is null ? null : FilePaths.ExtraFileData.AttachmentPath(attachment.Hash);
    }

    public async Task ExtractAttachmentsAsync(string ed2K)
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

        // Create temporary directory for extraction
        var tempDir = Path.Combine(Path.GetTempPath(), $"shizou_attachments_{Guid.NewGuid()}");
        Directory.CreateDirectory(tempDir);

        try
        {
            var attachmentsDir = FilePaths.ExtraFileData.AttachmentsDir;
            Directory.CreateDirectory(attachmentsDir);

            await _ffmpegService.ExtractAttachmentsAsync(fileInfo, tempDir).ConfigureAwait(false);

            foreach (var attachment in new DirectoryInfo(tempDir).EnumerateFiles())
            {
                var hash = (await _hashService.GetFileHashesAsync(attachment, HashIds.Crc32).ConfigureAwait(false))[HashIds.Crc32];
                var attachmentPath = FilePaths.ExtraFileData.AttachmentPath(hash);

                // Move to central location if it doesn't exist
                if (!File.Exists(attachmentPath)) File.Move(attachment.FullName, attachmentPath);

                // Save or update database record
                var existingAttachment = await context.LocalFileAttachments
                                                      .FirstOrDefaultAsync(a => a.LocalFileId == localFile.Id && a.Filename == attachment.Name)
                                                      .ConfigureAwait(false);

                if (existingAttachment is null)
                    context.LocalFileAttachments.Add(new()
                    {
                        LocalFileId = localFile.Id,
                        Filename = attachment.Name,
                        Hash = hash,
                    });
                else
                    existingAttachment.Hash = hash;
            }

            context.SaveChanges();
        }
        finally
        {
            // Clean up temp directory
            if (Directory.Exists(tempDir))
                try
                {
                    Directory.Delete(tempDir, true);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to delete temporary directory {TempDir}", tempDir);
                }
        }
    }
}
