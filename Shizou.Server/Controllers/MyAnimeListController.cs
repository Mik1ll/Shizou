using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Net.Http;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Threading.Tasks;
using System.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Shizou.Server.Options;
using Swashbuckle.AspNetCore.Annotations;

namespace Shizou.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class MyAnimeListController : ControllerBase
{
    private readonly ILogger<MyAnimeListController> _logger;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ShizouOptions _options;
    private static string? _codeChallengeAndVerifier;
    private static string? _state;

    public MyAnimeListController(ILogger<MyAnimeListController> logger, IHttpClientFactory httpClientFactory, IOptionsSnapshot<ShizouOptions> optionsSnapshot)
    {
        _logger = logger;
        _httpClientFactory = httpClientFactory;
        _options = optionsSnapshot.Value;
    }

    [HttpGet("Authenticate")]
    [SwaggerResponse(StatusCodes.Status400BadRequest)]
    [SwaggerResponse(StatusCodes.Status200OK)]
    public ActionResult Authenticate()
    {
        var uri = GetAuthenticationUrl(_options.MyAnimeList.ClientId, HttpContext.Connection.RemoteIpAddress!.ToString(), _logger);
        if (uri is null)
            return BadRequest();
        return Ok(uri.AbsoluteUri);
    }

    public static Uri? GetAuthenticationUrl(string clientId, string remoteIp, ILogger logger)
    {
        if (string.IsNullOrWhiteSpace(clientId))
        {
            logger.LogError("Tried to authenticate with MAL without a client ID");
            return null;
        }

        if (!AccountController.IsLoopBackAddress(remoteIp))
        {
            logger.LogError("Can only authenticate on localhost due to MAL auth redirect registration");
            return null;
        }

        _codeChallengeAndVerifier = GetCodeVerifier();
        _state = Guid.NewGuid().ToString();

        var authorizeUriBuilder = new UriBuilder("https://myanimelist.net/v1/oauth2/authorize");
        var query = HttpUtility.ParseQueryString(authorizeUriBuilder.Query);
        query["response_type"] = "code";
        query["client_id"] = clientId;
        query["state"] = _state;
        query["code_challenge"] = _codeChallengeAndVerifier;
        query["code_challenge_method"] = "plain";
        authorizeUriBuilder.Query = query.ToString();
        return authorizeUriBuilder.Uri;
    }

    [HttpGet("GetToken")]
    [SwaggerResponse(StatusCodes.Status400BadRequest)]
    [SwaggerResponse(StatusCodes.Status409Conflict)]
    [SwaggerResponse(StatusCodes.Status200OK)]
    public async Task<ActionResult> GetToken(string code, string? state)
    {
        if (string.IsNullOrWhiteSpace(_options.MyAnimeList.ClientId))
        {
            _logger.LogError("Tried to get MAL token without a client ID");
            return BadRequest("Tried to get MAL token without a client ID");
        }

        if (_codeChallengeAndVerifier is null)
        {
            _logger.LogError("Tried to get MAL token without a code verifier");
            return BadRequest("Tried to get MAL token without a code verifier");
        }

        if (string.IsNullOrWhiteSpace(state) || state != _state)
        {
            _logger.LogError("State is empty or did not match stored value");
            return BadRequest();
        }

        var httpClient = _httpClientFactory.CreateClient();
        var request = new HttpRequestMessage(HttpMethod.Post, "https://myanimelist.net/v1/oauth2/token")
        {
            Content = new FormUrlEncodedContent(new List<KeyValuePair<string, string>>
            {
                new("client_id", _options.MyAnimeList.ClientId),
                new("grant_type", "authorization_code"),
                new("code", code),
                new("code_verifier", _codeChallengeAndVerifier)
            })
        };
        var result = await httpClient.SendAsync(request);
        if (!result.IsSuccessStatusCode)
        {
            _logger.LogError("Something went wrong when getting token");
            return Conflict();
        }

        var token = await result.Content.ReadFromJsonAsync<TokenResponse>();
        if (token is null)
        {
            _logger.LogError("Couldn't get token from response body");
            return Conflict();
        }

        _options.MyAnimeList.MyAnimeListToken =
            new MyAnimeListToken(token.access_token, DateTimeOffset.UtcNow + TimeSpan.FromSeconds(token.expires_in), token.refresh_token);
        _options.SaveToFile();
        return Ok();
    }

    private async Task<bool> RefreshToken()
    {
        if (_options.MyAnimeList.MyAnimeListToken is null)
        {
            _logger.LogError("No token to refresh");
            return false;
        }

        var httpClient = _httpClientFactory.CreateClient();
        var request = new HttpRequestMessage(HttpMethod.Post, "https://myanimelist.net/v1/oauth2/token")
        {
            Content = new FormUrlEncodedContent(new List<KeyValuePair<string, string>>
            {
                new("client_id", _options.MyAnimeList.ClientId),
                new("grant_type", "refresh_token"),
                new("refresh_token", _options.MyAnimeList.MyAnimeListToken.RefreshToken)
            })
        };

        var result = await httpClient.SendAsync(request);
        if (!result.IsSuccessStatusCode) _logger.LogError("Something went wrong when refreshing token");

        var token = await result.Content.ReadFromJsonAsync<TokenResponse>();
        if (token is null)
        {
            _logger.LogError("Couldn't get token from response body");
            return false;
        }

        _options.MyAnimeList.MyAnimeListToken =
            new MyAnimeListToken(token.access_token, DateTimeOffset.UtcNow + TimeSpan.FromSeconds(token.expires_in), token.refresh_token);
        _options.SaveToFile();

        return true;
    }

    [SuppressMessage("ReSharper", "InconsistentNaming")]
    [SuppressMessage("ReSharper", "NotAccessedPositionalProperty.Local")]
    [SuppressMessage("ReSharper", "ClassNeverInstantiated.Local")]
    private record TokenResponse(string token_type, int expires_in, string access_token, string refresh_token);

    private static string GetCodeVerifier()
    {
        return Base64UrlTextEncoder.Encode(RandomNumberGenerator.GetBytes(32));
    }
}