﻿using System;
using System.IO;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Shizou.Data;
using Shizou.Data.CommandInputArgs;
using Shizou.Data.Database;
using Shizou.Server.Options;

namespace Shizou.Server.Services;

public class ImageService
{
    private readonly ILogger<ImageService> _logger;
    private readonly IShizouContextFactory _contextFactory;
    private readonly IOptionsMonitor<ShizouOptions> _optionsMonitor;
    private readonly CommandService _commandService;


    public ImageService(
        ILogger<ImageService> logger,
        IShizouContextFactory contextFactory,
        IOptionsMonitor<ShizouOptions> optionsMonitor,
        CommandService commandService)
    {
        _logger = logger;
        _contextFactory = contextFactory;
        _optionsMonitor = optionsMonitor;
        _commandService = commandService;
    }

    /// <summary>
    ///     Dispatches commands to retrieve missing anime posters.
    /// </summary>
    public void GetMissingAnimePosters()
    {
        var imageServer = _optionsMonitor.CurrentValue.AniDb.ImageServerHost;
        if (imageServer is null)
        {
            _logger.LogWarning("Image server is missing, aborting");
            return;
        }

        using var context = _contextFactory.CreateDbContext();
        var filenames = context.AniDbAnimes.Where(a => a.ImageFilename != null).Select(a => a.ImageFilename!).ToList();

        foreach (var (uri, name) in filenames.Select(p => (GetAniDbImageUri(imageServer, p), p)))
        {
            var path = FilePaths.AnimePosterPath(name);
            if (!File.Exists(path))
                _commandService.Dispatch(new GetImageArgs(uri, path));
        }
    }

    /// <summary>
    ///     Dispatch a command to retrieve a specific anime poster.
    /// </summary>
    /// <param name="animeId"></param>
    public void GetAnimePoster(int animeId)
    {
        var imageServer = _optionsMonitor.CurrentValue.AniDb.ImageServerHost;
        if (imageServer is null)
        {
            _logger.LogWarning("Image server is missing, aborting");
            return;
        }

        using var context = _contextFactory.CreateDbContext();
        var filename = context.AniDbAnimes.Where(a => a.Id == animeId).Select(a => a.ImageFilename).FirstOrDefault();
        if (filename is null)
        {
            _logger.LogWarning("Anime image for anime {AnimeId} does not exist, aborting", animeId);
            return;
        }

        var path = FilePaths.AnimePosterPath(filename);
        var uri = GetAniDbImageUri(imageServer, filename);
        _commandService.Dispatch(new GetImageArgs(uri, path));
    }

    /// <summary>
    ///     Try to get a stored thumbnail for the episode.
    /// </summary>
    /// <param name="episodeId"></param>
    /// <returns>The fileinfo of the thumbnail file if it exists</returns>
    public FileInfo? GetEpisodeThumbnail(int episodeId)
    {
        using var context = _contextFactory.CreateDbContext();
        var localFiles = context.LocalFiles.AsNoTracking().Include(lf => lf.ImportFolder).Where(lf =>
            lf.AniDbFile!.AniDbEpisodeFileXrefs.Any(ep => ep.AniDbEpisodeId == episodeId)).ToList();
        foreach (var localFile in localFiles)
            if (new FileInfo(FilePaths.ExtraFileData.ThumbnailPath(localFile.Ed2k)) is { Exists: true } thumbnail)
                return thumbnail;
        return null;
    }

    public void GetCreatorImage(int creatorId)
    {
        var imageServer = _optionsMonitor.CurrentValue.AniDb.ImageServerHost;
        if (imageServer is null)
        {
            _logger.LogWarning("Image server is missing, aborting");
            return;
        }

        using var context = _contextFactory.CreateDbContext();
        var filename = context.AniDbCreators.Where(a => a.Id == creatorId).Select(a => a.ImageFilename).FirstOrDefault();
        if (filename is null)
        {
            _logger.LogWarning("Creator image for id {CreatorId} does not exist, aborting", creatorId);
            return;
        }

        var path = FilePaths.CreatorImagePath(filename);
        var uri = GetAniDbImageUri(imageServer, filename);
        _commandService.Dispatch(new GetImageArgs(uri, path));
    }


    private string GetAniDbImageUri(string imageServer, string filename) =>
        new UriBuilder("https", imageServer, 443, $"images/main/{filename}").Uri.AbsoluteUri;
}
