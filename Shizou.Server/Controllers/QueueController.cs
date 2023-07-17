using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Shizou.Data.Enums;
using Shizou.Data.Models;
using Shizou.Server.CommandProcessors;
using Swashbuckle.AspNetCore.Annotations;

namespace Shizou.Server.Controllers;

[ApiController]
[Route("[controller]")]
public class QueueController : ControllerBase
{
    private readonly IList<CommandProcessor> _processors;

    public QueueController(IEnumerable<CommandProcessor> processors)
    {
        _processors = processors.ToList();
    }

    private CommandProcessor GetProcessor(QueueType queueType)
    {
        return _processors.First(p => p.QueueType == queueType);
    }

    [HttpPut("{queueType}/[action]")]
    [SwaggerResponse(StatusCodes.Status200OK)]
    [SwaggerResponse(StatusCodes.Status400BadRequest)]
    public ActionResult Pause(QueueType queueType)
    {
        GetProcessor(queueType).Pause();
        return Ok();
    }

    [HttpPut("{queueType}/[action]")]
    [SwaggerResponse(StatusCodes.Status200OK)]
    [SwaggerResponse(StatusCodes.Status409Conflict)]
    [SwaggerResponse(StatusCodes.Status400BadRequest)]
    public ActionResult Unpause(QueueType queueType)
    {
        var processor = GetProcessor(queueType);
        if (processor.Unpause())
            return Ok();
        else
            return Conflict($"Pause state locked: {processor.PauseReason}");
    }

    [SuppressMessage("ReSharper", "NotAccessedPositionalProperty.Global")]
    public record PauseResult(bool Paused, string? PauseReason);

    [HttpGet("{queueType}/[action]")]
    [SwaggerResponse(StatusCodes.Status200OK)]
    [SwaggerResponse(StatusCodes.Status400BadRequest)]
    [Produces("application/json")]
    public ActionResult<PauseResult> PauseState(QueueType queueType)
    {
        var processor = GetProcessor(queueType);
        return Ok(new PauseResult(processor.Paused, processor.PauseReason));
    }


    [HttpGet("{queueType}/[action]")]
    [SwaggerResponse(StatusCodes.Status200OK)]
    [SwaggerResponse(StatusCodes.Status204NoContent)]
    [SwaggerResponse(StatusCodes.Status400BadRequest)]
    [Produces("application/json")]
    public ActionResult<CommandRequest?> Current(QueueType queueType)
    {
        return Ok(GetProcessor(queueType).CurrentCommand);
    }

    [HttpGet("{queueType}/[action]")]
    [SwaggerResponse(StatusCodes.Status200OK)]
    [SwaggerResponse(StatusCodes.Status400BadRequest)]
    [Produces("application/json")]
    public ActionResult<IEnumerable<CommandRequest>> Queued(QueueType queueType)
    {
        return Ok(GetProcessor(queueType).GetQueuedCommands());
    }

    [HttpPut("{queueType}/[action]")]
    [SwaggerResponse(StatusCodes.Status200OK)]
    [SwaggerResponse(StatusCodes.Status400BadRequest)]
    public ActionResult Clear(QueueType queueType)
    {
        GetProcessor(queueType).ClearQueue();
        return Ok();
    }
}
