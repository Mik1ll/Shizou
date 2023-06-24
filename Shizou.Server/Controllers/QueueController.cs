using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Shizou.Common.Enums;
using Shizou.Data.Database;
using Shizou.Data.Models;
using Shizou.Server.CommandProcessors;
using Swashbuckle.AspNetCore.Annotations;

namespace Shizou.Server.Controllers;

[ApiController]
[Route("[controller]")]
public class QueueController : ControllerBase
{
    private readonly IEnumerable<CommandProcessor> _queues;
    private readonly ShizouContext _context;

    public QueueController(IEnumerable<CommandProcessor> queues, ShizouContext context)
    {
        _queues = queues;
        _context = context;
    }

    [HttpGet]
    [SwaggerResponse(StatusCodes.Status200OK)]
    [Produces("application/json")]
    public ActionResult List()
    {
        return Ok(_queues);
    }

    [HttpGet("{queueType}")]
    [SwaggerResponse(StatusCodes.Status200OK)]
    [SwaggerResponse(StatusCodes.Status404NotFound)]
    [Produces("application/json")]
    public ActionResult Get(QueueType queueType)
    {
        var queue = _queues.FirstOrDefault(q => q.QueueType == queueType);
        return queue is not null ? Ok(queue) : NotFound();
    }

    [HttpPut("{queueType}")]
    [SwaggerResponse(StatusCodes.Status200OK)]
    [SwaggerResponse(StatusCodes.Status404NotFound)]
    [SwaggerResponse(StatusCodes.Status409Conflict)]
    public ActionResult Pause(QueueType queueType, bool paused)
    {
        var queue = _queues.FirstOrDefault(q => q.QueueType == queueType);
        if (queue is null) return NotFound();
        if (paused)
            queue.Pause();
        else
            queue.Unpause();
        if (queue.Paused && !paused)
            return Conflict($"Pause state locked: {queue.PauseReason}");
        return Ok();
    }

    [HttpGet("{queueType}/current")]
    [SwaggerResponse(StatusCodes.Status200OK)]
    [SwaggerResponse(StatusCodes.Status404NotFound)]
    [SwaggerResponse(StatusCodes.Status204NoContent)]
    [Produces("application/json")]
    public ActionResult<CommandRequest?> GetCurrentCommand(QueueType queueType)
    {
        var queue = _queues.FirstOrDefault(q => q.QueueType == queueType);
        if (queue is null) return NotFound("Queue not found");
        return Ok(queue.CurrentCommand);
    }

    [HttpGet("{queueType}/queued")]
    [SwaggerResponse(StatusCodes.Status200OK)]
    [SwaggerResponse(StatusCodes.Status404NotFound)]
    [Produces("application/json")]
    public ActionResult<IEnumerable<CommandRequest>> GetQueuedCommands(QueueType queueType)
    {
        var queue = _queues.FirstOrDefault(q => q.QueueType == queueType);
        if (queue is null) return NotFound("Queue not found");
        return Ok(_context.CommandRequests.Where(cr => cr.QueueType == queueType));
    }
}