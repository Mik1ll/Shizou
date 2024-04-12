using System;
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

        foreach (var (uri, name) in filenames.Select(p => (GetAnimePosterUri(imageServer, p), p)))
        {
            var path = FilePaths.AnimePosterPath(name);
            if (!File.Exists(path))
                _commandService.Dispatch(new GetImageArgs(uri, path));
        }
    }

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
        var uri = GetAnimePosterUri(imageServer, filename);
        _commandService.Dispatch(new GetImageArgs(uri, path));
    }

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

    private string GetAnimePosterUri(string imageServer, string filename)
    {
        return new UriBuilder("https", imageServer, 443, $"images/main/{filename}").Uri.AbsoluteUri;
    }
}
