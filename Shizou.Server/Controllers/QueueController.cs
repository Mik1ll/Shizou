using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Shizou.Data.Enums;
using Shizou.Data.Models;
using Shizou.Server.Services;
using Swashbuckle.AspNetCore.Annotations;

namespace Shizou.Server.Controllers;

[ApiController]
[Route("[controller]")]
public class QueueController : ControllerBase
{
    private readonly QueueService _queueService;

    public QueueController(QueueService queueService)
    {
        _queueService = queueService;
    }

    [HttpPut("{queueType}/[action]")]
    [SwaggerResponse(StatusCodes.Status200OK)]
    [SwaggerResponse(StatusCodes.Status400BadRequest)]
    public ActionResult Pause(QueueType queueType)
    {
        _queueService.Pause(queueType);
        return Ok();
    }

    [HttpPut("{queueType}/[action]")]
    [SwaggerResponse(StatusCodes.Status200OK)]
    [SwaggerResponse(StatusCodes.Status409Conflict)]
    [SwaggerResponse(StatusCodes.Status400BadRequest)]
    public ActionResult Unpause(QueueType queueType)
    {
        if (_queueService.Unpause(queueType))
            return Ok();
        else
            return Conflict($"Pause state locked: {_queueService.GetPauseReason(queueType)}");
    }

    [SuppressMessage("ReSharper", "NotAccessedPositionalProperty.Global")]
    public record PauseResult(bool Paused, string? PauseReason);

    [HttpGet("{queueType}/[action]")]
    [SwaggerResponse(StatusCodes.Status200OK)]
    [SwaggerResponse(StatusCodes.Status400BadRequest)]
    [Produces("application/json")]
    public ActionResult<PauseResult> PauseState(QueueType queueType)
    {
        return Ok(new PauseResult(_queueService.GetPauseState(queueType), _queueService.GetPauseReason(queueType)));
    }


    [HttpGet("{queueType}/[action]")]
    [SwaggerResponse(StatusCodes.Status200OK)]
    [SwaggerResponse(StatusCodes.Status204NoContent)]
    [SwaggerResponse(StatusCodes.Status400BadRequest)]
    [Produces("application/json")]
    public ActionResult<CommandRequest?> Current(QueueType queueType)
    {
        return Ok(_queueService.GetCurrentCommand(queueType));
    }

    [HttpGet("{queueType}/[action]")]
    [SwaggerResponse(StatusCodes.Status200OK)]
    [SwaggerResponse(StatusCodes.Status400BadRequest)]
    [Produces("application/json")]
    public ActionResult<IEnumerable<CommandRequest>> Queued(QueueType queueType)
    {
        return Ok(_queueService.GetQueuedCommands(queueType));
    }
}
