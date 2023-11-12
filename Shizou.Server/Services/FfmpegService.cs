using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
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
            $"-v fatal -select_streams v:0 -show_entries format=duration:stream=duration -of csv \"{fileInfo.FullName}\"";
        process.Start();
        var durations = (await process.StandardOutput.ReadToEndAsync()).Split('\n', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
            .Select(s => s.Split(',')).ToDictionary(s => s[0], s => s[1]);
        if (durations.TryGetValue("format", out var formatDurStr) && double.TryParse(formatDurStr, out var formatDur))
            return formatDur;
        if (durations.TryGetValue("stream", out var streamDurStr) && double.TryParse(streamDurStr, out var streamDur))
            return streamDur;

        _logger.LogWarning("Could not get a duration for file \"{FilePath}\"", fileInfo.FullName);
        return null;
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
