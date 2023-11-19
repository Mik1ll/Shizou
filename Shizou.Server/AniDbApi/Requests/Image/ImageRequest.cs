using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Shizou.Server.AniDbApi.RateLimiters;

namespace Shizou.Server.AniDbApi.Requests.Image;

public class ImageRequest
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<ImageRequest> _logger;
    private readonly ImageRateLimiter _rateLimiter;

    public string? Url { get; set; }
    public string? SavePath { get; set; }

    public ImageRequest(ILogger<ImageRequest> logger, IHttpClientFactory httpClientFactory, ImageRateLimiter rateLimiter)
    {
        _httpClient = httpClientFactory.CreateClient();
        _logger = logger;
        _rateLimiter = rateLimiter;
    }

    public async Task ProcessAsync()
    {
        if (Url is null || SavePath is null)
            throw new ArgumentException($"Url/Save path not set before {nameof(ProcessAsync)} called");
        using (await _rateLimiter.AcquireAsync().ConfigureAwait(false))
        {
            _logger.LogInformation("Sending Image request: {Url}", Url);
            Directory.CreateDirectory(Path.GetDirectoryName(SavePath)!);
            var fileStream = new FileStream(SavePath, FileMode.Create);
            await using var _ = fileStream.ConfigureAwait(false);
            await (await _httpClient.GetStreamAsync(Url).ConfigureAwait(false)).CopyToAsync(fileStream).ConfigureAwait(false);
        }
    }
}
