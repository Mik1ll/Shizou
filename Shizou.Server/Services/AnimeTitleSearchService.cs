using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Shizou.Data;
using Shizou.Data.Database;
using Shizou.Data.Enums;
using Shizou.Data.Models;

namespace Shizou.Server.Services;

public class AnimeTitleSearchService
{
    private readonly ILogger<AnimeTitleSearchService> _logger;
    private readonly IHttpClientFactory _clientFactory;
    private readonly IDbContextFactory<ShizouContext> _contextFactory;

    public AnimeTitleSearchService(
        ILogger<AnimeTitleSearchService> logger,
        IHttpClientFactory clientFactory,
        IDbContextFactory<ShizouContext> contextFactory)
    {
        _logger = logger;
        _clientFactory = clientFactory;
        _contextFactory = contextFactory;
    }

    public async Task<string> GetTitles()
    {
        // ReSharper disable once MethodHasAsyncOverload
        // ReSharper disable once UseAwaitUsing
        using var context = _contextFactory.CreateDbContext();
        var timer = context.Timers.FirstOrDefault(t => t.Type == TimerType.AnimeTitlesRequest);
        if (timer is not null && timer.Expires > DateTime.UtcNow)
        {
            _logger.LogInformation("Anime titles requested recently, getting from local file");
            if (File.Exists(FilePaths.AnimeTitlesPath))
                return await File.ReadAllTextAsync(FilePaths.AnimeTitlesPath, Encoding.UTF8);
            _logger.LogError("Could not find anime titles file at \"{AnimeTitlesPath}\"", FilePaths.AnimeTitlesPath);
        }

        var rateLimit = TimeSpan.FromDays(1);
        var rateLimitExpires = DateTime.UtcNow - rateLimit;
        if (timer is not null)
            timer.Expires = rateLimitExpires;
        else
            context.Timers.Add(new Timer
            {
                Type = TimerType.AnimeTitlesRequest,
                ExtraId = null,
                Expires = rateLimitExpires
            });
        // ReSharper disable once MethodHasAsyncOverload
        context.SaveChanges();
        var client = _clientFactory.CreateClient("gzip");
        var result = await client.GetAsync("https://anidb.net/api/anime-titles.dat.gz");
        var data = await result.Content.ReadAsStringAsync();
        Directory.CreateDirectory(Path.GetDirectoryName(FilePaths.AnimeTitlesPath)!);
        await File.WriteAllTextAsync(FilePaths.AnimeTitlesPath, data, Encoding.UTF8);
        return data;
    }
}
