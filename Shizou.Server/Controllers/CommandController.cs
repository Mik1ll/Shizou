﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Shizou.Server.AniDbApi.Requests.Udp;
using Shizou.Server.Commands.AniDb;
using Shizou.Server.Services;
using Swashbuckle.AspNetCore.Annotations;

namespace Shizou.Server.Controllers;

[ApiController]
[Route("[controller]")]
public class CommandController : ControllerBase
{
    private readonly CommandService _commandService;
    private readonly IServiceProvider _provider;

    public CommandController(CommandService commandService, IServiceProvider provider)
    {
        _commandService = commandService;
        _provider = provider;
    }

    [HttpPut("UpdateMyList")]
    [SwaggerResponse(StatusCodes.Status200OK)]
    [Consumes("application/json")]
    public void UpdateMyList(UpdateMyListArgs commandArgs)
    {
        _commandService.Dispatch(commandArgs);
    }

    [HttpPut("GenericUdpRequest")]
    [Produces("text/plain")]
    [Consumes("application/json")]
    public async Task<string?> GenericUdpRequest(string command, Dictionary<string, string> args)
    {
        var req = new GenericRequest(_provider, command, args);
        await req.Process();
        return req.ResponseCodeString + "\n" + req.ResponseText;
    }
}
