﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Shizou.AniDbApi.Requests.Udp;
using Shizou.Commands.AniDb;
using Shizou.Services;

namespace Shizou.Controllers;

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
    [ProducesResponseType(StatusCodes.Status200OK)]
    public void UpdateMyList(UpdateMyListArgs commandArgs)
    {
        _commandService.Dispatch(commandArgs);
    }

    [HttpPut("MyListDelete")]
    public async Task MyListDeleteRequest(int aid, string epno)
    {
        var req = new MyListDeleteRequest(_provider, aid, epno);
        await req.Process();
    }

    [HttpPut("GenericUdpRequest")]
    public async Task<string?> GenericUdpRequest(string command, Dictionary<string, string> args)
    {
        var req = new GenericRequest(_provider, command, args);
        await req.Process();
        return req.ResponseCodeString + "\n" + req.ResponseText;
    }
}