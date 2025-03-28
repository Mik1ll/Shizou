﻿using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Shizou.Data;
using Shizou.Data.CommandInputArgs;
using Shizou.Data.Database;
using Shizou.Data.Enums;
using Shizou.Data.Models;
using Shizou.Server.Services;
using Swashbuckle.AspNetCore.Annotations;

namespace Shizou.Server.Controllers;

[ApiController]
[Route($"{Constants.ApiPrefix}/[controller]")]
public class LocalFiles : EntityGetController<LocalFile>
{
    private readonly CommandService _commandService;

    public LocalFiles(IShizouContext context, CommandService commandService) : base(context,
        file => file.Id)
    {
        _commandService = commandService;
    }

    [HttpPut("ProcessFile/{id:int}")]
    [SwaggerResponse(StatusCodes.Status200OK)]
    public Ok ProcessFile([FromRoute] int id)
    {
        _commandService.Dispatch(new ProcessArgs(id, IdTypeLocalOrFile.LocalId));
        return TypedResults.Ok();
    }
}
