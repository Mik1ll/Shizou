using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shizou.Server.Services;

public class FfmpegService
{
    public static async Task<List<(int Idx, string Codec, string FileName)>> GetFontStreams(FileInfo fileInfo, string[] validFontFormats)
    {
        using var streamsP = NewFfprobeProcess();
        streamsP.StartInfo.Arguments =
            $"-v fatal -select_streams t -show_entries stream=index,codec_name:stream_tags=filename -of csv=p=0 \"{fileInfo.FullName}\"";
        streamsP.Start();
        var streams = (await streamsP.StandardOutput.ReadToEndAsync()).Split('\n', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
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

    public static async Task ExtractFonts(FileInfo fileInfo, List<(int Idx, string Codec, string FileName)> fontStreams, string outputDir)
    {
        using var extractP = NewFfmpegProcess();
        extractP.StartInfo.Arguments =
            $"-v fatal -y {string.Join(" ", fontStreams.Select(s => $"-dump_attachment:{s.Idx} \"{Path.Combine(outputDir, s.FileName)}\""))} -i \"{fileInfo.FullName}\"";
        extractP.Start();
        await extractP.WaitForExitAsync();
    }

    public static async Task<List<(int Idx, string Codec, string FileName)>> GetSubtitleStreams(FileInfo fileInfo, string[] validSubFormats,
        Func<int, string> getFileName)
    {
        using var streamsP = NewFfprobeProcess();
        streamsP.StartInfo.Arguments = $"-v fatal -select_streams s -show_entries stream=index,codec_name -of csv=p=0 \"{fileInfo.FullName}\"";
        streamsP.Start();
        var streams = (await streamsP.StandardOutput.ReadToEndAsync()).Split('\n', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
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

    public static async Task ExtractSubtitles(FileInfo fileInfo, List<(int Idx, string Codec, string FileName)> subStreams, string outputDir)
    {
        var extractP = NewFfmpegProcess();
        extractP.StartInfo.Arguments =
            $"-v fatal -y -i \"{fileInfo.FullName}\" {string.Join(" ", subStreams.Select(s => $"-map 0:{s.Idx} -c ass \"{Path.Combine(outputDir, s.FileName)}\""))}";
        extractP.Start();
        await extractP.WaitForExitAsync();
    }

    private static Process NewFfprobeProcess()
    {
        var streamsP = new Process();
        streamsP.StartInfo.UseShellExecute = false;
        streamsP.StartInfo.CreateNoWindow = true;
        streamsP.StartInfo.RedirectStandardOutput = true;
        streamsP.StartInfo.StandardOutputEncoding = Encoding.UTF8;
        streamsP.StartInfo.FileName = "ffprobe";
        return streamsP;
    }

    private static Process NewFfmpegProcess()
    {
        var ffmpegProcess = new Process();
        ffmpegProcess.StartInfo.UseShellExecute = false;
        ffmpegProcess.StartInfo.CreateNoWindow = true;
        ffmpegProcess.StartInfo.FileName = "ffmpeg";
        return ffmpegProcess;
    }
}
