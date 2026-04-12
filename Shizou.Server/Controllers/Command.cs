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
public class Command(
    CommandService commandService,
    IPingRequest pingRequest,
    IGenericRequest genericRequest
) : ControllerBase
{
    [HttpPut("[action]")]
    [SwaggerResponse(StatusCodes.Status200OK)]
    public Ok UpdateMyList([FromBody] UpdateMyListArgs commandArgs)
    {
        commandService.Dispatch(commandArgs);
        return TypedResults.Ok();
    }

    [HttpPut("[action]")]
    [SwaggerResponse(StatusCodes.Status200OK)]
    public Ok SyncMyList()
    {
        commandService.Dispatch(new SyncMyListArgs());
        return TypedResults.Ok();
    }

    [HttpPut("[action]")]
    [SwaggerResponse(StatusCodes.Status200OK)]
    public async Task<Ok> PingAniDb()
    {
        pingRequest.SetParameters();
        await pingRequest.ProcessAsync().ConfigureAwait(false);
        return TypedResults.Ok();
    }

    [HttpPut("[action]")]
    [SwaggerResponse(StatusCodes.Status200OK, null, typeof(string), MediaTypeNames.Text.Plain)]
    [SwaggerResponse(StatusCodes.Status424FailedDependency)]
    public async Task<Results<Ok<string>, ProblemHttpResult>> GenericUdpRequest(string command, Dictionary<string, string> args)
    {
        genericRequest.SetParameters(command, args);
        var resp = await genericRequest.ProcessAsync().ConfigureAwait(false);
        if (resp is null)
            return TypedResults.Problem("AniDB did not respond.", statusCode: StatusCodes.Status424FailedDependency);
        return TypedResults.Ok(resp.ResponseCodeText + "\n" + resp.ResponseText);
    }
}
