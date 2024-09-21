﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Shizou.Data.Database;
using Shizou.Data.Enums;
using Shizou.Server.Extensions.Query;
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
            _logger.LogWarning("Collection View is disabled, will not update");
            return;
        }

        if (string.IsNullOrWhiteSpace(options.CollectionView.Path))
        {
            _logger.LogWarning("Collection View path is null or whitespace, cannot update view");
            return;
        }

        var collectionDir = new DirectoryInfo(options.CollectionView.Path);
        Directory.CreateDirectory(collectionDir.FullName);

        using var context = _contextFactory.CreateDbContext();
        var anime = context.AniDbAnimes.HasLocalFiles().Select(a => new { a.Id, Title = a.TitleTranscription, a.AnimeType, a.EpisodeCount })
            .ToDictionary(a => a.Id, a => a);

        var localFiles = context.LocalFiles.Where(lf => lf.ImportFolderId != null && lf.AniDbFileId != null).Select(lf => new
        {
            lf.AniDbFileId,
            lf.ImportFolder!.Path,
            lf.PathTail,
            lf.Crc,
            Episodes = lf.AniDbFile!.AniDbEpisodes.Select(ep => new
            {
                ep.Id,
                ep.TitleEnglish,
                ep.AniDbAnimeId
            }).ToList()
        }).ToList();

        List<(string Target, string Path)> linkPaths = localFiles.SelectMany(f => f.Episodes.Select(ep => ep.AniDbAnimeId).Distinct().Select(aid => (
                Path.GetFullPath(Path.Combine(f.Path, f.PathTail)),
                Path.Combine(options.CollectionView.Path,
                    anime[aid] is { AnimeType: not AnimeType.TvSeries, EpisodeCount: 1 } &&
                    f.Episodes.SingleOrDefault(e => e.AniDbAnimeId == aid)?.TitleEnglish is
                        "Movie" or "Complete Movie" or "Short Movie" or "OVA" or "OAD" or "Web" or "Special"
                        ? "Movies"
                        : "Shows",
                    $"{InvalidCharRegex.Replace(anime[aid].Title, "_")} [anidb-{aid}]",
                    $"{InvalidCharRegex.Replace(anime[aid].Title, "_")} [anidb-{f.AniDbFileId}] [{f.Crc}]{Path.GetExtension(f.PathTail)}"))))
            .ToList();

        var pathsHashSet = linkPaths.Select(p => p.Path).ToHashSet(RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
            ? StringComparer.InvariantCultureIgnoreCase
            : StringComparer.InvariantCulture);

        var toDelete = collectionDir.EnumerateFiles("*", SearchOption.AllDirectories)
            .Where(f => f.LinkTarget != null && (!pathsHashSet.Contains(f.FullName) || !File.Exists(f.LinkTarget)))
            .ToList();

        foreach (var file in toDelete)
            file.Delete();

        RemoveEmptyDirs(collectionDir);

        var toCreate = linkPaths.Where(file => File.Exists(file.Target) && !File.Exists(file.Path)).ToList();

        foreach (var file in toCreate)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(file.Path)!);
            File.CreateSymbolicLink(file.Path, file.Target);
        }
    }

    private void RemoveEmptyDirs(DirectoryInfo dirInfo)
    {
        foreach (var dir in dirInfo.EnumerateDirectories())
        {
            RemoveEmptyDirs(dir);
            if (!dir.EnumerateFileSystemInfos().Any())
                dir.Delete(false);
        }
    }
}
