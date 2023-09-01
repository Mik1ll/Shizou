using System;
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
[Route("api/[controller]")]
public class CommandController : ControllerBase
{
    private readonly CommandService _commandService;
    private readonly UdpRequestFactory _udpRequestFactory;

    public CommandController(CommandService commandService, UdpRequestFactory udpRequestFactory)
    {
        _commandService = commandService;
        _udpRequestFactory = udpRequestFactory;
    }

    [HttpPut("UpdateMyList")]
    [SwaggerResponse(StatusCodes.Status200OK)]
    [Consumes("application/json")]
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

    [HttpPut("ScheduleExport")]
    [SwaggerResponse(StatusCodes.Status200OK)]
    public void ScheduleExport()
    {
        _commandService.Dispatch(new ExportArgs());
    }

    [HttpPut("PollExport")]
    [SwaggerResponse(StatusCodes.Status200OK)]
    public void PollExport()
    {
        _commandService.Dispatch(new ExportPollArgs());
    }

    [HttpPut("SyncMyListFromExport")]
    [SwaggerResponse(StatusCodes.Status200OK)]
    public void SyncMyListFromExport(string filename)
    {
        _commandService.Dispatch(new SyncMyListFromExportArgs(filename));
    }

    [HttpPut("GenericUdpRequest")]
    [Produces("text/plain")]
    [Consumes("application/json")]
    public async Task<string?> GenericUdpRequest(string command, Dictionary<string, string> args)
    {
        var req = _udpRequestFactory.GenericRequest(command, args);
        await req.Process();
        return req.ResponseCodeString + "\n" + req.ResponseText;
    }

    [HttpPut("RestoreMyListBackup")]
    [SwaggerResponse(StatusCodes.Status200OK)]
    public void RestoreMyListBackup(string date)
    {
        _commandService.Dispatch(new RestoreMyListBackupArgs(DateOnly.Parse(date)));
    }
}
