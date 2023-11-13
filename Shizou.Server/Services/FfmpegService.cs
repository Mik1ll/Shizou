using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Shizou.Server.Services;

public class FfmpegService
{
    private readonly ILogger<FfmpegService> _logger;

    public FfmpegService(ILogger<FfmpegService> logger)
    {
        _logger = logger;
    }

    public async Task<List<(int Idx, string Codec, string FileName)>> GetFontStreams(FileInfo fileInfo, string[] validFontFormats)
    {
        using var process = NewFfprobeProcess();
        process.StartInfo.Arguments =
            $"-v fatal -select_streams t -show_entries stream=index,codec_name:stream_tags=filename -of csv=p=0 \"{fileInfo.FullName}\"";
        process.Start();
        var streams = (await process.StandardOutput.ReadToEndAsync()).Split('\n', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
            .Select(s =>
            {
                var split = s.Split(',');
                return split switch
                {
                    { Length: 3 } => (Idx: int.Parse(split[0]), Codec: split[1], FileName: split[2]),
                    { Length: 2 } => (Idx: int.Parse(split[0]), Codec: split[1], FileName: string.Empty),
                    { Length: 1 } => (Idx: int.Parse(split[0]), Codec: string.Empty, FileName: string.Empty),
                    _ => throw new ArgumentOutOfRangeException(nameof(split.Length))
                };
            });

        var validStreams = streams.Where(s =>
            !string.IsNullOrEmpty(s.FileName) && (validFontFormats.Contains(s.Codec, StringComparer.OrdinalIgnoreCase) ||
                                                  validFontFormats.Any(f => s.FileName.EndsWith(f, StringComparison.OrdinalIgnoreCase)))).ToList();
        return validStreams;
    }

    public async Task ExtractFonts(FileInfo fileInfo, List<(int Idx, string Codec, string FileName)> fontStreams, string outputDir)
    {
        Directory.CreateDirectory(outputDir);
        using var process = NewFfmpegProcess();
        process.StartInfo.Arguments =
            $"-v fatal -y {string.Join(" ", fontStreams.Select(s => $"-dump_attachment:{s.Idx} \"{Path.Combine(outputDir, s.FileName)}\""))} -i \"{fileInfo.FullName}\"";
        process.Start();
        await process.WaitForExitAsync();
    }

    public async Task<List<(int Idx, string Codec, string FileName)>> GetSubtitleStreams(FileInfo fileInfo, string[] validSubFormats,
        Func<int, string> getFileName)
    {
        using var process = NewFfprobeProcess();
        process.StartInfo.Arguments = $"-v fatal -select_streams s -show_entries stream=index,codec_name -of csv=p=0 \"{fileInfo.FullName}\"";
        process.Start();
        var streams = (await process.StandardOutput.ReadToEndAsync()).Split('\n', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
            .Select(s =>
            {
                var split = s.Split(',');
                return split switch
                {
                    { Length: 2 } => (Idx: int.Parse(split[0]), Codec: split[1], FileName: getFileName(int.Parse(split[0]))),
                    { Length: 1 } => (Idx: int.Parse(split[0]), Codec: string.Empty, FileName: string.Empty),
                    _ => throw new ArgumentOutOfRangeException(nameof(split.Length))
                };
            });
        var validStreams = streams.Where(s => validSubFormats.Contains(s.Codec, StringComparer.OrdinalIgnoreCase)).ToList();
        return validStreams;
    }

    public async Task ExtractSubtitles(FileInfo fileInfo, List<(int Idx, string Codec, string FileName)> subStreams, string outputDir)
    {
        Directory.CreateDirectory(outputDir);
        using var process = NewFfmpegProcess();
        process.StartInfo.Arguments =
            $"-v fatal -y -i \"{fileInfo.FullName}\" {string.Join(" ", subStreams.Select(s => $"-map 0:{s.Idx} -c ass \"{Path.Combine(outputDir, s.FileName)}\""))}";
        process.Start();
        await process.WaitForExitAsync();
    }

    public async Task<double?> GetDuration(FileInfo fileInfo)
    {
        using var process = NewFfprobeProcess();
        process.StartInfo.Arguments =
            $"-v fatal -select_streams V -show_entries format=duration:stream=duration:stream_tags -sexagesimal -of json \"{fileInfo.FullName}\"";
        process.Start();
        using var doc = await JsonDocument.ParseAsync(process.StandardOutput.BaseStream);
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
        string TrimFractional(string durStr)
        {
            return durStr.LastIndexOf('.') is var idx && idx >= 0 && durStr.Length - 1 >= idx + 8 ? durStr.Remove(idx + 8) : durStr;
        }
    }

    public async Task ExtractThumbnail(FileInfo fileInfo, double duration, string outputPath)
    {
        if (Path.GetDirectoryName(outputPath) is { Length: > 0 } parentPath)
            Directory.CreateDirectory(parentPath);
        using var process = NewFfmpegProcess();
        var fps = 3;
        var thumbnailWindow = 30;
        var height = 480;
        process.StartInfo.Arguments =
            $"-v fatal -y -ss {duration * .4} -t {thumbnailWindow} -i \"{fileInfo.FullName}\" -map 0:V:0 " +
            $"-vf \"fps={fps},select='min(eq(selected_n,0)+gt(scene,0.4),1)',thumbnail={thumbnailWindow * fps},scale=-2:{height}\" " +
            $"-frames:v 1 -pix_fmt yuv420p -c:v libwebp -compression_level 6 -preset drawing \"{outputPath}\"";
        process.Start();
        await process.WaitForExitAsync();
    }

    public async Task<bool> HasVideo(FileInfo fileInfo)
    {
        using var process = NewFfprobeProcess();
        process.StartInfo.Arguments = $"-v fatal -select_streams V -show_entries stream=codec_name -of csv \"{fileInfo.FullName}\"";
        process.Start();
        var hasVideo = await process.StandardOutput.ReadToEndAsync();
        return !string.IsNullOrWhiteSpace(hasVideo);
    }

    private Process NewFfprobeProcess()
    {
        var streamsP = new Process();
        streamsP.StartInfo.UseShellExecute = false;
        streamsP.StartInfo.CreateNoWindow = true;
        streamsP.StartInfo.RedirectStandardOutput = true;
        streamsP.StartInfo.StandardOutputEncoding = Encoding.UTF8;
        streamsP.StartInfo.FileName = "ffprobe";
        return streamsP;
    }

    private Process NewFfmpegProcess()
    {
        var ffmpegProcess = new Process();
        ffmpegProcess.StartInfo.UseShellExecute = false;
        ffmpegProcess.StartInfo.CreateNoWindow = true;
        ffmpegProcess.StartInfo.FileName = "ffmpeg";
        return ffmpegProcess;
    }
}
