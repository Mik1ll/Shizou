using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Shizou.Data;
using Shizou.Data.Models;
using Shizou.Server.Services;
using Swashbuckle.AspNetCore.Annotations;

namespace Shizou.Server.Controllers;

[ApiController]
[Route($"{Constants.ApiPrefix}/[controller]")]
public class MyAnimeList : ControllerBase
{
    private readonly ILogger<MyAnimeList> _logger;
    private readonly MyAnimeListService _myAnimeListService;

    public MyAnimeList(ILogger<MyAnimeList> logger, MyAnimeListService myAnimeListService)
    {
        _logger = logger;
        _myAnimeListService = myAnimeListService;
    }

    [HttpGet("[action]")]
    [SwaggerResponse(StatusCodes.Status400BadRequest)]
    [SwaggerResponse(StatusCodes.Status200OK, type: typeof(string))]
    public Results<Ok<string>, BadRequest> Authenticate()
    {
        var baseUri = new UriBuilder(HttpContext.Request.Scheme, HttpContext.Request.Host.Host, HttpContext.Request.Host.Port ?? -1,
            HttpContext.Request.PathBase).Uri;
        var url = _myAnimeListService.MalAuthorization.GetAuthenticationUrl(baseUri);
        if (url is null)
            return TypedResults.BadRequest();
        return TypedResults.Ok(url);
    }

    [HttpGet("[action]")]
    [SwaggerResponse(StatusCodes.Status400BadRequest)]
    [SwaggerResponse(StatusCodes.Status409Conflict)]
    [SwaggerResponse(StatusCodes.Status200OK)]
    public async Task<Results<RedirectHttpResult, BadRequest, Conflict>> GetToken([FromQuery] string code, [FromQuery] string? state)
    {
        if (string.IsNullOrWhiteSpace(state))
        {
            _logger.LogError("State was not provided");
            return TypedResults.BadRequest();
        }

        if (!await _myAnimeListService.MalAuthorization.GetNewTokenAsync(code, state).ConfigureAwait(false))
            return TypedResults.Conflict();
        var baseUri = new UriBuilder(HttpContext.Request.Scheme, HttpContext.Request.Host.Host, HttpContext.Request.Host.Port ?? -1,
            HttpContext.Request.PathBase).Uri;
        return TypedResults.LocalRedirect(baseUri.LocalPath);
    }

    [HttpPost("[action]/{animeId:int}")]
    public async Task<Results<Ok<MalAnime>, BadRequest>> UpdateAnime([FromRoute] int animeId)
    {
        var result = await _myAnimeListService.GetAnimeAsync(animeId).ConfigureAwait(false);
        if (result is not null)
            return TypedResults.Ok(result);
        return TypedResults.BadRequest();
    }
}
