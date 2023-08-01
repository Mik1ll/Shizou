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

    public void GetMissingAnimePosters()
    {
        using var context = _contextFactory.CreateDbContext();
        var animeImagePaths = context.AniDbAnimes.Where(a => a.ImagePath != null).Select(a => a.ImagePath!).ToList();
        var imageServer = _optionsMonitor.CurrentValue.AniDb.ImageServerHost;
        if (imageServer is null)
        {
            _logger.LogWarning("Image server is missing, aborting");
            return;
        }
        foreach (var (uri, name) in animeImagePaths.Select(p => (new UriBuilder("http", imageServer, 80, p).Uri.AbsoluteUri, p)))
        {
            var path = Path.Combine(FilePaths.AnimePostersDir, name);
            if (!File.Exists(path))
                _commandService.Dispatch(new GetImageCommandArgs(uri, path));
        }
    }
}
