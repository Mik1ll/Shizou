﻿using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Shizou.Data;
using Shizou.Data.Database;

namespace Shizou.Server.Services;

public class SubtitleService
{
    private readonly ILogger<SubtitleService> _logger;
    private readonly IDbContextFactory<ShizouContext> _contextFactory;

    private readonly string[] _validSubFormats = { "ass", "ssa", "srt", "webvtt", "subrip", "ttml", "text", "mov_text", "dvb_teletext" };

    public SubtitleService(ILogger<SubtitleService> logger, IDbContextFactory<ShizouContext> contextFactory)
    {
        _logger = logger;
        _contextFactory = contextFactory;
    }

    public async Task ExtractSubtitles(int localFileId)
    {
        // ReSharper disable once UseAwaitUsing
        // ReSharper disable once MethodHasAsyncOverload
        using var context = _contextFactory.CreateDbContext();
        var localFile = context.LocalFiles.Include(e => e.ImportFolder).FirstOrDefault(e => e.Id == localFileId);
        if (localFile is null)
            return;
        if (localFile.ImportFolder is null)
        {
            _logger.LogWarning("Tried to get local file {LocalFileId} with no import folder", localFileId);
            return;
        }

        var subsDir = Path.Combine(FilePaths.ExtraFileDataDir, localFile.Ed2k, "ExtractedSubs");
        Directory.CreateDirectory(subsDir);
        var fullPath = Path.GetFullPath(Path.Combine(localFile.ImportFolder.Path, localFile.PathTail));
        var fileInfo = new FileInfo(fullPath);
        if (!fileInfo.Exists)
        {
            _logger.LogWarning("Local file path \"{FullPath}\" does not exist", fullPath);
            return;
        }

        using var streamsP = new Process();
        streamsP.StartInfo.UseShellExecute = false;
        streamsP.StartInfo.CreateNoWindow = true;
        streamsP.StartInfo.RedirectStandardOutput = true;
        streamsP.StartInfo.StandardOutputEncoding = Encoding.UTF8;
        streamsP.StartInfo.FileName = "ffprobe";
        streamsP.StartInfo.Arguments = $"-v fatal -select_streams s -show_entries stream=index,codec_name -of csv=p=0 \"{fileInfo.FullName}\"";
        streamsP.Start();
        var streams = (await streamsP.StandardOutput.ReadToEndAsync()).Split('\n', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
            .Select(s =>
            {
                var split = s.Split(',');
                return split switch
                {
                    { Length: 2 } => (Idx: split[0], Codec: split[1]),
                    { Length: 1 } => (Idx: split[0], Codec: string.Empty),
                    _ => throw new ArgumentOutOfRangeException(nameof(string.Length))
                };
            });
        var validStreams = streams.Where(s => _validSubFormats.Contains(s.Codec)).ToList();
        if (validStreams.Count <= 0)
        {
            _logger.LogDebug("No valid streams for {LocalFileId}, skipping subtitle extraction", localFileId);
            return;
        }

        using var extractP = new Process();
        extractP.StartInfo.UseShellExecute = false;
        extractP.StartInfo.CreateNoWindow = true;
        extractP.StartInfo.FileName = "ffmpeg";
        extractP.StartInfo.Arguments = $"-v fatal -y -i \"{fileInfo.FullName}\" " +
                                       string.Join(" ", validStreams.Select(s => $"-map 0:{s.Idx} -c ass \"{Path.Combine(subsDir, $"{s.Idx}.ass")}\""));
        extractP.Start();
        await extractP.WaitForExitAsync();
    }
}
