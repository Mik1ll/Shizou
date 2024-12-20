﻿using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Shizou.Data;
using Shizou.Data.CommandInputArgs;
using Shizou.Server.AniDbApi.Requests.Udp.Interfaces;
using Shizou.Server.Services;
using Swashbuckle.AspNetCore.Annotations;

namespace Shizou.Server.Controllers;

[ApiController]
[Route($"{Constants.ApiPrefix}/[controller]")]
public class Command : ControllerBase
{
    private readonly CommandService _commandService;
    private readonly IGenericRequest _genericRequest;
    private readonly IPingRequest _pingRequest;

    public Command(
        CommandService commandService,
        IGenericRequest genericRequest,
        IPingRequest pingRequest
    )
    {
        _commandService = commandService;
        _genericRequest = genericRequest;
        _pingRequest = pingRequest;
    }

    [HttpPut("UpdateMyList")]
    [SwaggerResponse(StatusCodes.Status200OK)]
    public void UpdateMyList(UpdateMyListArgs commandArgs)
    {
        _commandService.Dispatch(commandArgs);
    }

    [HttpPut("SyncMyList")]
    [SwaggerResponse(StatusCodes.Status200OK)]
    public void SyncMyList()
    {
        _commandService.Dispatch(new SyncMyListArgs());
    }

    [HttpPut("PingAniDb")]
    public async Task PingAniDb()
    {
        _pingRequest.SetParameters();
        await _pingRequest.ProcessAsync().ConfigureAwait(false);
    }
}
