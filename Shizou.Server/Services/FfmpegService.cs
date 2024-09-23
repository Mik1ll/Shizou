using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Shizou.Data;
using Shizou.Data.Models;
using Shizou.Server.Options;

namespace Shizou.Server.Services;

public class FfmpegService
{
    private static readonly string[] ValidSubFormats = ["ass", "ssa", "srt", "webvtt", "subrip", "ttml", "text", "mov_text", "dvb_teletext"];
    private readonly ILogger<FfmpegService> _logger;
    private readonly IOptionsMonitor<ShizouOptions> _optionsMonitor;

    public FfmpegService(ILogger<FfmpegService> logger, IOptionsMonitor<ShizouOptions> optionsMonitor)
    {
        _logger = logger;
        _optionsMonitor = optionsMonitor;
    }

    /// <summary>
    ///     Dump the file's attachments to the output directory
    /// </summary>
    /// <param name="fileInfo">Target file</param>
    /// <param name="outputDir">Output directory</param>
    public async Task ExtractAttachmentsAsync(FileInfo fileInfo, string outputDir)
    {
        Directory.CreateDirectory(outputDir);
        using var process = NewFfmpegProcess(["-v", "fatal", "-y", "-dump_attachment:t", "", "-i", fileInfo.FullName]);
        process.StartInfo.WorkingDirectory = outputDir;
        process.Start();
        await process.WaitForExitAsync().ConfigureAwait(false);
    }

    /// <summary>
    ///     Extract and convert compatible subtitle streams to ASS subtitles
    /// </summary>
    /// <param name="localFile">The file to extract subtitles from</param>
    public async Task ExtractSubtitlesAsync(LocalFile localFile)
    {
        _logger.LogInformation("Extracting subtitles for local file id: {LocalFileId}", localFile.Id);

        if (GetLocalFileInfo(localFile) is not { } fileInfo)
            return;

        var subsDir = FilePaths.ExtraFileData.SubsDir(localFile.Ed2k);
        Directory.CreateDirectory(subsDir);

        var subStreams = (await GetStreamsAsync(fileInfo).ConfigureAwait(false)).Where(s => ValidSubFormats.Contains(s.codec))
            .Select(s => (s.index, filename: Path.GetFileName(FilePaths.ExtraFileData.SubPath(localFile.Ed2k, s.index)))).ToList();

        if (subStreams.Count <= 0)
        {
            _logger.LogDebug("No valid streams for {LocalFileId}, skipping subtitle extraction", localFile.Id);
            return;
        }

        using var process = NewFfmpegProcess([
            "-v", "fatal", "-y",
            "-i", fileInfo.FullName,
            .. subStreams.SelectMany(IEnumerable<string> (s) => ["-map", $"0:{s.index}", "-c", "ass", Path.Combine(subsDir, s.filename)])
        ]);
        process.Start();
        await process.WaitForExitAsync().ConfigureAwait(false);
    }

    /// <summary>
    ///     Create a thumbnail from the file's video stream
    /// </summary>
    /// <param name="localFile">The file to extract a thumbnail from</param>
    public async Task ExtractThumbnailAsync(LocalFile localFile)
    {
        if (GetLocalFileInfo(localFile) is not { } fileInfo)
            return;

        _logger.LogInformation("Extracting thumbnail for local file id: {LocalFileId}", localFile.Id);

        if (!await HasVideoAsync(fileInfo).ConfigureAwait(false))
        {
            _logger.LogInformation("File at \"{FilePath}\" has no video stream", fileInfo.FullName);
            return;
        }

        var duration = await GetDurationAsync(fileInfo).ConfigureAwait(false);
        if (duration is null)
        {
            _logger.LogWarning("Failed to get duration of video for file at \"{FilePath}\"", fileInfo.FullName);
            return;
        }

        var outputPath = FilePaths.ExtraFileData.ThumbnailPath(localFile.Ed2k);
        if (Path.GetDirectoryName(outputPath) is { } parentPath)
            Directory.CreateDirectory(parentPath);
        var fps = 3;
        var thumbnailWindow = 30;
        var height = 480;
        var width = 854;
        using var process = NewFfmpegProcess([
            "-v", "fatal", "-y",
            "-ss", (duration * .4).Value.ToString(CultureInfo.InvariantCulture),
            "-t", thumbnailWindow.ToString(),
            "-i", fileInfo.FullName,
            "-map", "0:V:0",
            "-vf", $"fps={fps},select='min(eq(selected_n,0)+gt(scene,0.4),1)',thumbnail={thumbnailWindow * fps},scale=-2:{height},crop='min({width},iw)'",
            "-frames:v", "1", "-pix_fmt", "yuv420p",
            "-c:v", "libwebp",
            "-compression_level", "6",
            "-preset", "drawing",
            outputPath
        ]);
        process.Start();
        await process.WaitForExitAsync().ConfigureAwait(false);

        var outputInfo = new FileInfo(outputPath);
        if (outputInfo is not { Exists: true, Length: > 0 })
            outputInfo.Delete();
    }

    /// <summary>
    ///     Get the stream information of a file
    /// </summary>
    /// <param name="fileInfo">The file to inspect</param>
    /// <returns>A list of stream information</returns>
    public async Task<List<(int index, string codec, string? lang, string? title, string? filename)>> GetStreamsAsync(FileInfo fileInfo)
    {
        using var p = NewFfprobeProcess([
            "-v", "fatal",
            "-show_entries", "stream=index,codec_name : stream_tags=language,title,filename",
            "-of", "json=c=1",
            fileInfo.FullName
        ]);
        p.Start();
        List<(int index, string codec, string? lang, string? title, string? filename)> streams = [];
        using var document = JsonDocument.Parse(await p.StandardOutput.ReadToEndAsync().ConfigureAwait(false));
        if (!document.RootElement.TryGetProperty("streams", out var streamsEl))
            return streams;
        foreach (var streamEl in streamsEl.EnumerateArray())
        {
            var index = streamEl.GetProperty("index").GetInt32();
            var codec = string.Empty;
            if (streamEl.TryGetProperty("codec_name", out var codecEl))
                codec = codecEl.GetString() ?? string.Empty;

            string? filename = null;
            string? lang = null;
            string? title = null;
            if (streamEl.TryGetProperty("tags", out var tagsEl))
            {
                if (tagsEl.TryGetProperty("filename", out var filenameEl))
                    filename = filenameEl.GetString();
                if (tagsEl.TryGetProperty("language", out var langEl))
                    lang = langEl.GetString();
                if (tagsEl.TryGetProperty("title", out var titleEl))
                    title = titleEl.GetString();
            }

            var stream = (index, codec, lang, title, filename);
            streams.Add(stream);
        }

        return streams;
    }

    /// <summary>
    ///     Try to get <see cref="FileInfo" /> for a local file
    /// </summary>
    /// <param name="localFile"></param>
    /// <returns>The <see cref="FileInfo" /> if it exists, else null</returns>
    private FileInfo? GetLocalFileInfo(LocalFile localFile)
    {
        if (localFile.ImportFolder is null)
        {
            _logger.LogWarning("Tried to get local file {LocalFileId} with no import folder", localFile.Id);
            return null;
        }

        var fullPath = Path.GetFullPath(Path.Combine(localFile.ImportFolder.Path, localFile.PathTail));
        var fileInfo = new FileInfo(fullPath);
        if (!fileInfo.Exists)
        {
            _logger.LogWarning("Local file path \"{FullPath}\" does not exist", fullPath);
            return null;
        }

        return fileInfo;
    }

    /// <summary>
    ///     Try to get the duration of a media file
    /// </summary>
    /// <param name="fileInfo">The file to inspect</param>
    /// <returns>The duration in seconds, if it exists</returns>
    private async Task<double?> GetDurationAsync(FileInfo fileInfo)
    {
        using var process = NewFfprobeProcess([
            "-v", "fatal",
            "-select_streams", "V",
            "-show_entries", "format=duration:stream=duration:stream_tags",
            "-sexagesimal", "-of", "json",
            fileInfo.FullName
        ]);
        process.Start();
        using var doc = await JsonDocument.ParseAsync(process.StandardOutput.BaseStream).ConfigureAwait(false);
        var rootEl = doc.RootElement;
        if (rootEl.TryGetProperty("streams", out var streamsEl))
            foreach (var stream in streamsEl.EnumerateArray())
            {
                if (stream.TryGetProperty("tags", out var tagsEl))
                    foreach (var tagDurProp in tagsEl.EnumerateObject().Where(p => p.Name.StartsWith("duration", StringComparison.OrdinalIgnoreCase)))
                        if (tagDurProp.Value.GetString() is { } durStr)
                            if (TimeSpan.TryParse(TrimFractional(durStr), CultureInfo.InvariantCulture, out var tagDur))
                                return tagDur.TotalSeconds;

                if (rootEl.TryGetProperty("duration", out var streamDurEl))
                    if (streamDurEl.GetString() is { } streamDurStr)
                        if (TimeSpan.TryParse(TrimFractional(streamDurStr), CultureInfo.InvariantCulture, out var streamDur))
                            return streamDur.TotalSeconds;
            }

        if (rootEl.TryGetProperty("format", out var formatEl))
            if (formatEl.TryGetProperty("duration", out var formatDurEl))
                if (formatDurEl.GetString() is { } formatDurStr)
                    if (TimeSpan.TryParse(TrimFractional(formatDurStr), CultureInfo.InvariantCulture, out var formatDur))
                        return formatDur.TotalSeconds;

        _logger.LogWarning("Could not get a duration for file \"{FilePath}\"", fileInfo.FullName);
        return null;

        // Trim fractional value https://learn.microsoft.com/en-us/dotnet/api/system.timespan.parse?view=net-7.0#notes-to-callers
        string TrimFractional(string durStr) =>
            durStr.LastIndexOf('.') is var idx && idx >= 0 && durStr.Length - 1 >= idx + 8 ? durStr.Remove(idx + 8) : durStr;
    }

    /// <summary>
    ///     Check if a file has a video stream
    /// </summary>
    /// <param name="fileInfo">The file to inspect</param>
    /// <returns>True if the file has a vieo stream</returns>
    private async Task<bool> HasVideoAsync(FileInfo fileInfo)
    {
        using var process = NewFfprobeProcess([
            "-v", "fatal",
            "-select_streams", "V",
            "-show_entries", "stream=codec_name",
            "-of", "csv",
            fileInfo.FullName
        ]);
        process.Start();
        var hasVideo = await process.StandardOutput.ReadToEndAsync().ConfigureAwait(false);
        return !string.IsNullOrWhiteSpace(hasVideo);
    }

    private Process NewFfprobeProcess(IEnumerable<string> arguments)
    {
        var ffprobePath = _optionsMonitor.CurrentValue.Import.FfprobePath;
        ffprobePath = string.IsNullOrWhiteSpace(ffprobePath) ? "ffprobe" : ffprobePath;
        var startInfo = new ProcessStartInfo(ffprobePath, arguments)
        {
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            WorkingDirectory = FilePaths.InstallDir
        };
        var ffprobeProcess = new Process() { StartInfo = startInfo };
        return ffprobeProcess;
    }

    private Process NewFfmpegProcess(IEnumerable<string> arguments)
    {
        var ffmpegPath = _optionsMonitor.CurrentValue.Import.FfmpegPath;
        ffmpegPath = string.IsNullOrWhiteSpace(ffmpegPath) ? "ffmpeg" : ffmpegPath;
        var startInfo = new ProcessStartInfo(ffmpegPath, arguments)
        {
            UseShellExecute = false,
            CreateNoWindow = true,
            WorkingDirectory = FilePaths.InstallDir
        };
        var ffmpegProcess = new Process() { StartInfo = startInfo };
        return ffmpegProcess;
    }
}
