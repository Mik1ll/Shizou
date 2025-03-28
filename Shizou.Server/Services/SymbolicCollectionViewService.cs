﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Shizou.Data.Database;
using Shizou.Server.Options;

namespace Shizou.Server.Services;

public class SymbolicCollectionViewService
{
    private static readonly Regex InvalidCharRegex = new('[' + Regex.Escape(string.Join("", Path.GetInvalidFileNameChars())) + ']', RegexOptions.Compiled);
    private readonly IShizouContextFactory _contextFactory;
    private readonly IOptionsMonitor<ShizouOptions> _optionsMonitor;
    private readonly ILogger<SymbolicCollectionViewService> _logger;

    public SymbolicCollectionViewService(IShizouContextFactory contextFactory, IOptionsMonitor<ShizouOptions> optionsMonitor,
        ILogger<SymbolicCollectionViewService> logger)
    {
        _contextFactory = contextFactory;
        _optionsMonitor = optionsMonitor;
        _logger = logger;
    }

    public void Update()
    {
        var options = _optionsMonitor.CurrentValue;
        if (!options.CollectionView.Enabled)
        {
            _logger.LogDebug("Collection View is disabled, will not update");
            return;
        }

        if (string.IsNullOrWhiteSpace(options.CollectionView.Path))
        {
            _logger.LogWarning("Collection View path is null or whitespace, cannot update view");
            return;
        }

        var (pathComparer, pathComparison) = OperatingSystem.IsWindows() || OperatingSystem.IsMacOS()
            ? (StringComparer.OrdinalIgnoreCase, StringComparison.OrdinalIgnoreCase)
            : (StringComparer.Ordinal, StringComparison.Ordinal);

        var collectionDir = Directory.CreateDirectory(options.CollectionView.Path);
        var eLinks = collectionDir.EnumerateFiles("*", SearchOption.AllDirectories).ToDictionary(f => f.FullName, pathComparer);
        var eDirs = new HashSet<string>(pathComparer);
        foreach (var file in eLinks.Values)
        {
            if (file.LinkTarget is null)
            {
                _logger.LogError("Non-symbolic link found inside the collection view: \"{FilePath}\", cancelling update", file.FullName);
                return;
            }

            eDirs.Add(file.DirectoryName!);
        }

        using var context = _contextFactory.CreateDbContext();

        var localFiles = context.LocalFiles.Where(lf => lf.ImportFolderId != null && lf.AniDbFileId != null).Select(lf => new
        {
            lf.AniDbFileId,
            lf.ImportFolder!.Path,
            lf.PathTail,
            lf.Crc,
            Episodes = lf.AniDbFile!.AniDbEpisodes.Select(ep => new
            {
                ep.Id,
                ep.AniDbAnimeId,
                AnimeTitle = ep.AniDbAnime.TitleTranscription,
            }).ToList(),
        }).ToList();

        var linkPaths = localFiles.SelectMany(f => f.Episodes.DistinctBy(e => e.AniDbAnimeId).Select(e =>
        {
            var title = InvalidCharRegex.Replace(e.AnimeTitle, "_");
            var link = Path.Combine(collectionDir.FullName, $"{title} [anidb-{e.AniDbAnimeId}]",
                $"{title} [anidb-{f.AniDbFileId}] [{f.Crc}]{Path.GetExtension(f.PathTail)}");
            var linkTarget = Path.Combine(f.Path, f.PathTail);
            return (linkTarget, link);
        })).Where(lp => File.Exists(lp.linkTarget));

        foreach (var lp in linkPaths)
        {
            var linkExists = eLinks.Remove(lp.link, out var fi);
            if (linkExists && !string.Equals(fi!.LinkTarget, lp.linkTarget, pathComparison))
            {
                fi.Delete();
                linkExists = false;
            }

            if (!linkExists)
            {
                if (Path.GetDirectoryName(lp.link) is { } dir && !eDirs.Contains(dir))
                    Directory.CreateDirectory(dir);
                try
                {
                    File.CreateSymbolicLink(lp.link, lp.linkTarget);
                }
                catch (IOException ex)
                {
                    _logger.LogError(ex, "Failed to create symbolic link for \"{Link}\" to \"{LinkTarget}\"", lp.link, lp.linkTarget);
                }
            }
        }

        foreach (var l in eLinks.Values)
            if (l.LinkTarget is not null)
                l.Delete();

        RemoveEmptyDirs(collectionDir);
    }

    private void RemoveEmptyDirs(DirectoryInfo dirInfo)
    {
        foreach (var dir in Directory.EnumerateDirectories(dirInfo.FullName, "*", SearchOption.AllDirectories).OrderByDescending(f => f.Length))
            if (!Directory.EnumerateFileSystemEntries(dir).Any())
                Directory.Delete(dir);
    }
}
