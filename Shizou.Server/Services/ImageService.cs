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
        return new UriBuilder("https", imageServer, 443, Path.Combine("images/main", filename)).Uri.AbsoluteUri;
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
        var animeImageNames = context.AniDbAnimes.Where(a => a.ImagePath != null).Select(a => a.ImagePath!).ToList();

        foreach (var (uri, name) in animeImageNames.Select(p => (GetAnimePosterUri(imageServer, p), p)))
        {
            var path = Path.Combine(FilePaths.AnimePostersDir, name);
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
        var imageName = context.AniDbAnimes.Where(a => a.Id == animeId).Select(a => a.ImagePath).FirstOrDefault();
        if (imageName is null)
        {
            _logger.LogWarning("Anime image for anime {AnimeId} does not exist, aborting", animeId);
            return;
        }
        var path = Path.Combine(FilePaths.AnimePostersDir, imageName);
        var uri = GetAnimePosterUri(imageServer, imageName);
        _commandService.Dispatch(new GetImageCommandArgs(uri, path));
    }
}
