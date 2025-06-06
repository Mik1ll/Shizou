﻿using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Shizou.Data;
using Shizou.Data.CommandInputArgs;
using Shizou.Data.Enums;
using Shizou.Data.Models;
using Shizou.Server.CommandProcessors;
using Shizou.Server.Commands;
using Swashbuckle.AspNetCore.Annotations;

namespace Shizou.Server.Controllers;

[ApiController]
[Route($"{Constants.ApiPrefix}/[controller]")]
public class Queues : ControllerBase
{
    private readonly IList<CommandProcessor> _processors;

    public Queues(IEnumerable<CommandProcessor> processors)
    {
        _processors = processors.ToList();
    }

    [HttpPut("{queueType}/[action]")]
    [SwaggerResponse(StatusCodes.Status200OK)]
    [SwaggerResponse(StatusCodes.Status400BadRequest)]
    public Ok Pause([FromRoute] QueueType queueType)
    {
        GetProcessor(queueType).Pause();
        return TypedResults.Ok();
    }

    [HttpPut("{queueType}/[action]")]
    [SwaggerResponse(StatusCodes.Status200OK)]
    [SwaggerResponse(StatusCodes.Status409Conflict, type: typeof(ProblemDetails))]
    [SwaggerResponse(StatusCodes.Status400BadRequest)]
    public Results<Ok, Conflict<string>> Unpause([FromRoute] QueueType queueType)
    {
        var processor = GetProcessor(queueType);
        if (processor.Unpause())
            return TypedResults.Ok();
        else
            return TypedResults.Conflict($"Pause state locked: {processor.PauseReason}");
    }

    [HttpGet("{queueType}/[action]")]
    [SwaggerResponse(StatusCodes.Status200OK, type: typeof(PauseResult))]
    [SwaggerResponse(StatusCodes.Status400BadRequest)]
    public Ok<PauseResult> PauseState([FromRoute] QueueType queueType)
    {
        var processor = GetProcessor(queueType);
        return TypedResults.Ok(new PauseResult(processor.Paused, processor.PauseReason));
    }


    [HttpGet("{queueType}/[action]")]
    [SwaggerResponse(StatusCodes.Status200OK, type: typeof(ICommand<CommandArgs>))]
    [SwaggerResponse(StatusCodes.Status204NoContent)]
    [SwaggerResponse(StatusCodes.Status400BadRequest)]
    public Ok<ICommand<CommandArgs>?> Current([FromRoute] QueueType queueType)
    {
        return TypedResults.Ok<ICommand<CommandArgs>?>(GetProcessor(queueType).CurrentCommand);
    }

    [HttpGet("{queueType}/[action]")]
    [SwaggerResponse(StatusCodes.Status200OK, type: typeof(List<CommandRequest>))]
    [SwaggerResponse(StatusCodes.Status400BadRequest)]
    public Ok<List<CommandRequest>> QueuedRequests([FromRoute] QueueType queueType)
    {
        return TypedResults.Ok(GetProcessor(queueType).GetQueuedCommands());
    }

    [HttpPut("{queueType}/[action]")]
    [SwaggerResponse(StatusCodes.Status200OK)]
    [SwaggerResponse(StatusCodes.Status400BadRequest)]
    public Ok Clear([FromRoute] QueueType queueType)
    {
        GetProcessor(queueType).ClearQueue();
        return TypedResults.Ok();
    }

    private CommandProcessor GetProcessor(QueueType queueType)
    {
        return _processors.First(p => p.QueueType == queueType);
    }

    [SuppressMessage("ReSharper", "NotAccessedPositionalProperty.Global")]
    public record PauseResult(bool Paused, string? PauseReason);
}
