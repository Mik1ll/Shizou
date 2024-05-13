using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using RHashWrapper;
using Shizou.Data;
using Shizou.Data.Database;

namespace Shizou.Server.Services;

public class SubtitleService
{
    private static Dictionary<string, List<AttachmentPath>>? _attachmentHashMap;
    private static Dictionary<AttachmentPath, string>? _attachmentHashMapReverse;
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

    public static async Task<string> GetAttachmentPathAsync(string ed2K, string fileName)
    {
        await PopulateAttachmentHashMapAsync().ConfigureAwait(false);
        var attachmentPath = new AttachmentPath(ed2K, fileName);
        if (_attachmentHashMapReverse!.TryGetValue(attachmentPath, out var hash))
        {
            var newPath = GetOrAddAttachmentHashMap(hash, attachmentPath);
            return FilePaths.ExtraFileData.AttachmentPath(newPath.Ed2K, newPath.Filename);
        }

        return FilePaths.ExtraFileData.AttachmentPath(ed2K, fileName);
    }

    private static async Task SaveAttachmentHashMapAsync()
    {
        var stream = new FileInfo(FilePaths.ExtraFileData.AttachmentHashMapPath).OpenWrite();
        await using (stream.ConfigureAwait(false))
        {
            await JsonSerializer.SerializeAsync(stream, _attachmentHashMap).ConfigureAwait(false);
        }
    }

    private static async Task PopulateAttachmentHashMapAsync()
    {
        if (_attachmentHashMap is null || _attachmentHashMapReverse is null)
        {
            if (new FileInfo(FilePaths.ExtraFileData.AttachmentHashMapPath) is { Exists: true } attachmentLinkTargetsFileInfo)
            {
                var stream = attachmentLinkTargetsFileInfo.OpenRead();
                await using (stream.ConfigureAwait(false))
                {
                    try
                    {
                        _attachmentHashMap = await JsonSerializer.DeserializeAsync<Dictionary<string, List<AttachmentPath>>>(stream).ConfigureAwait(false);
                        _attachmentHashMapReverse = _attachmentHashMap!
                            .SelectMany(kvp => kvp.Value.Select(v => new { kvp.Key, Value = v }))
                            .ToDictionary(kvp => kvp.Value, kvp => kvp.Key);
                    }
                    catch (JsonException)
                    {
                        _attachmentHashMap = new Dictionary<string, List<AttachmentPath>>();
                        _attachmentHashMapReverse = new Dictionary<AttachmentPath, string>();
                    }
                }
            }
            else
            {
                _attachmentHashMap = new Dictionary<string, List<AttachmentPath>>();
                _attachmentHashMapReverse = new Dictionary<AttachmentPath, string>();
            }
        }
    }

    private static AttachmentPath GetOrAddAttachmentHashMap(string hash, AttachmentPath attachmentPath)
    {
        var resolvedPath = attachmentPath;
        if (_attachmentHashMap!.TryGetValue(hash, out var attachmentPaths))
        {
            if (!attachmentPaths.Contains(attachmentPath))
                attachmentPaths.Add(attachmentPath);
            _attachmentHashMapReverse![attachmentPath] = hash;
            foreach (var path in attachmentPaths)
                if (File.Exists(FilePaths.ExtraFileData.AttachmentPath(path.Ed2K, path.Filename)))
                {
                    if (path != attachmentPath)
                        File.Delete(FilePaths.ExtraFileData.AttachmentPath(attachmentPath.Ed2K, attachmentPath.Filename));
                    resolvedPath = path;
                    attachmentPaths.Remove(path);
                    attachmentPaths.Insert(0, path);
                    break;
                }
        }
        else
        {
            _attachmentHashMap[hash] = new List<AttachmentPath> { attachmentPath };
            _attachmentHashMapReverse![attachmentPath] = hash;
        }

        return resolvedPath;
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

        var attachmentsDir = FilePaths.ExtraFileData.AttachmentsDir(ed2K);
        Directory.CreateDirectory(attachmentsDir);

        await _ffmpegService.ExtractAttachmentsAsync(fileInfo, attachmentsDir).ConfigureAwait(false);
        await PopulateAttachmentHashMapAsync().ConfigureAwait(false);
        foreach (var attachment in new DirectoryInfo(attachmentsDir).EnumerateFiles())
        {
            var hash = (await _hashService.GetFileHashesAsync(attachment, HashIds.Crc32).ConfigureAwait(false))[HashIds.Crc32];
            GetOrAddAttachmentHashMap(hash, new AttachmentPath(ed2K, attachment.Name));
        }

        await SaveAttachmentHashMapAsync().ConfigureAwait(false);
    }

    private record AttachmentPath(string Ed2K, string Filename);
}
