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
    ///     Extract thumbnail and subtitles from a media file in a single ffmpeg call
    /// </summary>
    /// <param name="localFile">The file to extract from</param>
    public async Task ExtractThumbnailAndSubtitlesAsync(LocalFile localFile)
    {
        if (GetLocalFileInfo(localFile) is not { } fileInfo)
            return;

        _logger.LogInformation("Starting extraction for local file id: {LocalFileId} at \"{Path}\"", localFile.Id, fileInfo.FullName);

        // Get video info and subtitle streams in parallel
        var probeStart = DateTimeOffset.Now;
        var videoInfoTask = GetVideoInfoAsync(fileInfo);
        var streamsTask = GetStreamsAsync(fileInfo);

        await Task.WhenAll(videoInfoTask, streamsTask).ConfigureAwait(false);

        var (hasVideo, duration) = await videoInfoTask.ConfigureAwait(false);
        var streams = await streamsTask.ConfigureAwait(false);
        var probeDuration = DateTimeOffset.Now - probeStart;

        _logger.LogDebug("Probing completed in {Seconds:F2}s - HasVideo: {HasVideo}, Duration: {Duration:F1}s, TotalStreams: {StreamCount}",
                         probeDuration.TotalSeconds, hasVideo, duration, streams.Count);

        // Prepare thumbnail extraction
        string? thumbnailPath = null;
        double thumbnailOffset = 0;
        if (hasVideo)
        {
            thumbnailPath = FilePaths.ExtraFileData.ThumbnailPath(localFile.Ed2k);
            if (Path.GetDirectoryName(thumbnailPath) is { } parentPath)
                Directory.CreateDirectory(parentPath);
            thumbnailOffset = Math.Min(duration * .50, Math.Max(Math.Min(duration * .20, 300), 40));
            _logger.LogDebug("Thumbnail will be extracted at offset {Offset:F1}s", thumbnailOffset);
        }
        else
        {
            _logger.LogInformation("No video stream found, skipping thumbnail extraction");
        }

        // Prepare subtitle streams
        var subsDir = FilePaths.ExtraFileData.SubsDir(localFile.Ed2k);
        Directory.CreateDirectory(subsDir);

        var subStreams = streams.Where(s => ValidSubFormats.Contains(s.codec))
                                .Select(s => (s.index, filename: Path.GetFileName(FilePaths.ExtraFileData.SubPath(localFile.Ed2k, s.index)))).ToList();

        if (subStreams.Count > 0)
        {
            _logger.LogDebug("Found {SubtitleCount} subtitle stream(s) to extract", subStreams.Count);
        }
        else
        {
            _logger.LogDebug("No valid subtitle streams found");
            if (!hasVideo)
            {
                _logger.LogWarning("No video or subtitle streams to extract, aborting");
                return;
            }
        }

        // Build ffmpeg command with two separate inputs to avoid performance issues
        // Input 0: With seek for thumbnail (fast seek to offset)
        // Input 1: Without seek for subtitles (efficient full-file processing)
        var args = new List<string> { "-v", "fatal", "-y" };

        if (thumbnailPath != null) args.AddRange(["-ss", thumbnailOffset.ToString("F3", CultureInfo.InvariantCulture), "-i", fileInfo.FullName]);

        if (subStreams.Count > 0) args.AddRange(["-i", fileInfo.FullName]);

        // Map thumbnail output from first input
        if (thumbnailPath != null)
            args.AddRange([
                              "-map", "0:V:0",
                              "-vf", "fps=5,thumbnail=50,scale=-2:480,crop='min(854,iw)'",
                              "-frames:v", "1",
                              "-c:v", "libwebp",
                              "-compression_level", "6",
                              "-preset", "drawing",
                              thumbnailPath,
                          ]);

        // Map subtitle outputs from appropriate input
        if (subStreams.Count > 0)
        {
            var subtitleInputIndex = thumbnailPath != null ? 1 : 0;
            args.AddRange(subStreams.SelectMany(s => new[] { "-map", $"{subtitleInputIndex}:{s.index}", "-c", "ass", Path.Combine(subsDir, s.filename) }));
        }

        _logger.LogDebug("Beginning extraction with ffmpeg");

        var extractionStart = DateTimeOffset.Now;
        using var process = NewFfmpegProcess(args);
        process.Start();
        await process.WaitForExitAsync().ConfigureAwait(false);
        var extractionDuration = DateTimeOffset.Now - extractionStart;

        var exitCode = process.ExitCode;
        if (exitCode != 0) _logger.LogWarning("ffmpeg process exited with code {ExitCode} for file \"{Filename}\"", exitCode, fileInfo.Name);

        // Verify thumbnail output
        if (thumbnailPath != null)
        {
            var outputInfo = new FileInfo(thumbnailPath);
            if (outputInfo is { Exists: true, Length: > 0 })
            {
                _logger.LogDebug("Thumbnail created successfully: {Size} bytes at \"{Path}\"", outputInfo.Length, thumbnailPath);
            }
            else
            {
                _logger.LogWarning("Thumbnail extraction failed or produced empty file");
                outputInfo.Delete();
            }
        }

        // Log subtitle extraction results
        if (subStreams.Count > 0)
        {
            var extractedCount = subStreams.Count(s => File.Exists(Path.Combine(subsDir, s.filename)));
            _logger.LogDebug("Extracted {ExtractedCount}/{TotalCount} subtitle file(s)", extractedCount, subStreams.Count);
        }

        _logger.LogInformation("Extraction for \"{Filename}\" completed in {Seconds:F2}s (thumbnail: {HasThumbnail}, subtitles: {SubtitleCount})",
                               fileInfo.Name, extractionDuration.TotalSeconds, thumbnailPath != null, subStreams.Count);
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
                                            fileInfo.FullName,
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
    ///     Get video information (duration and presence of video stream) in a single ffprobe call
    /// </summary>
    /// <param name="fileInfo">The file to inspect</param>
    /// <returns>A tuple containing whether the file has a video stream and the duration in seconds</returns>
    private async Task<(bool hasVideo, double duration)> GetVideoInfoAsync(FileInfo fileInfo)
    {
        using var process = NewFfprobeProcess([
                                                  "-v", "fatal",
                                                  "-select_streams", "V",
                                                  "-show_entries", "format=duration:stream=codec_name,duration:stream_tags",
                                                  "-sexagesimal", "-of", "json",
                                                  fileInfo.FullName,
                                              ]);
        process.Start();
        using var doc = await JsonDocument.ParseAsync(process.StandardOutput.BaseStream).ConfigureAwait(false);
        var rootEl = doc.RootElement;

        // Check for video stream
        var hasVideo = false;
        if (rootEl.TryGetProperty("streams", out var streamsEl) && streamsEl.GetArrayLength() > 0)
            foreach (var stream in streamsEl.EnumerateArray())
                if (stream.TryGetProperty("codec_name", out var codecEl) && !string.IsNullOrWhiteSpace(codecEl.GetString()))
                {
                    hasVideo = true;
                    break;
                }

        // Get duration
        double duration = 0;
        if (rootEl.TryGetProperty("streams", out streamsEl))
            foreach (var stream in streamsEl.EnumerateArray())
            {
                if (stream.TryGetProperty("tags", out var tagsEl))
                    foreach (var tagDurProp in tagsEl.EnumerateObject().Where(p => p.Name.StartsWith("duration", StringComparison.OrdinalIgnoreCase)))
                        if (tagDurProp.Value.GetString() is { } durStr)
                            if (TimeSpan.TryParse(TrimFractional(durStr), CultureInfo.InvariantCulture, out var tagDur))
                            {
                                duration = tagDur.TotalSeconds;
                                return (hasVideo, duration);
                            }

                if (stream.TryGetProperty("duration", out var streamDurEl))
                    if (streamDurEl.GetString() is { } streamDurStr)
                        if (TimeSpan.TryParse(TrimFractional(streamDurStr), CultureInfo.InvariantCulture, out var streamDur))
                        {
                            duration = streamDur.TotalSeconds;
                            return (hasVideo, duration);
                        }
            }

        if (rootEl.TryGetProperty("format", out var formatEl))
            if (formatEl.TryGetProperty("duration", out var formatDurEl))
                if (formatDurEl.GetString() is { } formatDurStr)
                    if (TimeSpan.TryParse(TrimFractional(formatDurStr), CultureInfo.InvariantCulture, out var formatDur))
                    {
                        duration = formatDur.TotalSeconds;
                        return (hasVideo, duration);
                    }

        if (duration == 0)
            _logger.LogWarning("Could not get a duration for file \"{FilePath}\"", fileInfo.FullName);

        return (hasVideo, duration);

        // Trim fractional value https://learn.microsoft.com/en-us/dotnet/api/system.timespan.parse?view=net-7.0#notes-to-callers
        string TrimFractional(string durStr) =>
            durStr.LastIndexOf('.') is var idx && idx >= 0 && durStr.Length - 1 >= idx + 8 ? durStr.Remove(idx + 8) : durStr;
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
                            WorkingDirectory = FilePaths.InstallDir,
                        };
        var ffprobeProcess = new Process { StartInfo = startInfo };
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
                            WorkingDirectory = FilePaths.InstallDir,
                        };
        var ffmpegProcess = new Process { StartInfo = startInfo };
        return ffmpegProcess;
    }
}
