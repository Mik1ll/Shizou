﻿using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Shizou.Server.Services;
using Swashbuckle.AspNetCore.Annotations;

namespace Shizou.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class MyAnimeListController : ControllerBase
{
    private readonly ILogger<MyAnimeListController> _logger;
    private readonly MyAnimeListService _myAnimeListService;

    public MyAnimeListController(ILogger<MyAnimeListController> logger, MyAnimeListService myAnimeListService)
    {
        _logger = logger;
        _myAnimeListService = myAnimeListService;
    }

    [HttpGet("Authenticate")]
    [SwaggerResponse(StatusCodes.Status400BadRequest)]
    [SwaggerResponse(StatusCodes.Status200OK)]
    public ActionResult Authenticate()
    {
        var uri = _myAnimeListService.GetAuthenticationUrl(HttpContext.Connection.RemoteIpAddress!.ToString());
        if (uri is null)
            return BadRequest();
        return Ok(uri.AbsoluteUri);
    }

    [HttpGet("GetToken")]
    [SwaggerResponse(StatusCodes.Status400BadRequest)]
    [SwaggerResponse(StatusCodes.Status409Conflict)]
    [SwaggerResponse(StatusCodes.Status200OK)]
    public async Task<ActionResult> GetToken(string code, string? state)
    {
        if (string.IsNullOrWhiteSpace(state))
        {
            _logger.LogError("State was not provided");
            return BadRequest();
        }

        if (!await _myAnimeListService.GetToken(code, state))
            return Conflict();
        return Ok();
    }
    
}