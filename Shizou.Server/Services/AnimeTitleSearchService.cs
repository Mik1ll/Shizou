using System;
using System.IO;
using System.IO.Compression;
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

    public async Task<string?> GetContent()
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
            return null;
        }

        var rateLimit = TimeSpan.FromDays(1);
        var rateLimitExpires = DateTime.UtcNow + rateLimit;
        if (timer is not null)
            timer.Expires = rateLimitExpires;
        else
            context.Timers.Add(new Timer
            {
                Type = TimerType.AnimeTitlesRequest,
                Expires = rateLimitExpires
            });
        // ReSharper disable once MethodHasAsyncOverload
        context.SaveChanges();
        // Not using gzip client, didn't work. Maybe it requires a gzip content header in the response
        var client = _clientFactory.CreateClient(string.Empty);
        client.DefaultRequestHeaders.UserAgent.ParseAdd("Shizou");
        using var response = await client.GetAsync("https://anidb.net/api/anime-titles.dat.gz");
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("Anime titles request failed: {StatusCode} {StatusMessage}", response.StatusCode, response.ReasonPhrase);
            return null;
        }

        await using var httpStream = await response.Content.ReadAsStreamAsync();
        await using var decompStream = new GZipStream(httpStream, CompressionMode.Decompress);
        using var reader = new StreamReader(decompStream);
        var data = await reader.ReadToEndAsync();
        Directory.CreateDirectory(Path.GetDirectoryName(FilePaths.AnimeTitlesPath)!);
        await File.WriteAllTextAsync(FilePaths.AnimeTitlesPath, data, Encoding.UTF8);
        return data;
    }

    public void ParseContent(string content)
    {
    }
}
