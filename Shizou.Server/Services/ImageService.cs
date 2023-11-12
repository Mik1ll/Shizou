using System;
using System.IO;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Shizou.Data;
using Shizou.Data.Database;
using Shizou.Server.Commands.AniDb;
using Shizou.Server.Options;

namespace Shizou.Server.Services;

public class ImageService
{
    private readonly ILogger<ImageService> _logger;
    private readonly IDbContextFactory<ShizouContext> _contextFactory;
    private readonly IOptionsMonitor<ShizouOptions> _optionsMonitor;
    private readonly CommandService _commandService;


    public ImageService(ILogger<ImageService> logger, IDbContextFactory<ShizouContext> contextFactory, IOptionsMonitor<ShizouOptions> optionsMonitor,
        CommandService commandService)
    {
        _logger = logger;
        _contextFactory = contextFactory;
        _optionsMonitor = optionsMonitor;
        _commandService = commandService;
    }

    private string GetAnimePosterUri(string imageServer, string filename)
    {
        return new UriBuilder("https", imageServer, 443, $"images/main/{filename}").Uri.AbsoluteUri;
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
            var path = GetAnimePosterPath(name);
            if (!File.Exists(path))
                _commandService.Dispatch(new GetImageCommandArgs(uri, path));
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
        var path = GetAnimePosterPath(filename);
        var uri = GetAnimePosterUri(imageServer, filename);
        _commandService.Dispatch(new GetImageCommandArgs(uri, path));
    }

    public static string GetAnimePosterPath(string imageFilename)
    {
        return Path.Combine(FilePaths.AnimePostersDir, imageFilename);
    }
}
