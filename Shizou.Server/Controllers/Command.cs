using System.Collections.Generic;
using System.Net.Mime;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
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
    private readonly IPingRequest _pingRequest;
    private readonly IGenericRequest _genericRequest;

    public Command(
        CommandService commandService,
        IPingRequest pingRequest,
        IGenericRequest genericRequest
    )
    {
        _commandService = commandService;
        _pingRequest = pingRequest;
        _genericRequest = genericRequest;
    }

    [HttpPut("[action]")]
    [SwaggerResponse(StatusCodes.Status200OK)]
    public Ok UpdateMyList([FromBody] UpdateMyListArgs commandArgs)
    {
        _commandService.Dispatch(commandArgs);
        return TypedResults.Ok();
    }

    [HttpPut("[action]")]
    [SwaggerResponse(StatusCodes.Status200OK)]
    public Ok SyncMyList()
    {
        _commandService.Dispatch(new SyncMyListArgs());
        return TypedResults.Ok();
    }

    [HttpPut("[action]")]
    [SwaggerResponse(StatusCodes.Status200OK)]
    public async Task<Ok> PingAniDb()
    {
        _pingRequest.SetParameters();
        await _pingRequest.ProcessAsync().ConfigureAwait(false);
        return TypedResults.Ok();
    }

    [HttpPut("[action]")]
    [SwaggerResponse(StatusCodes.Status200OK, null, typeof(string), MediaTypeNames.Text.Plain)]
    public async Task<string> GenericUdpRequest(string command, Dictionary<string, string> args)
    {
        _genericRequest.SetParameters(command, args);
        var resp = await _genericRequest.ProcessAsync().ConfigureAwait(false);
        if (resp is not null)
            return resp.ResponseCodeText + "\n" + resp.ResponseText;
        return string.Empty;
    }
}
