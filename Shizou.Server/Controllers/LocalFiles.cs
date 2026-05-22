using System.Collections.Generic;
using Microsoft.AspNetCore.Http;
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
public class LocalFiles(IShizouContext context, CommandService commandService) : ControllerBase
{
    [HttpGet]
    [SwaggerResponse(StatusCodes.Status200OK, type: typeof(List<LocalFile>))]
    public Ok<List<LocalFile>> GetAll() => EntityEndpoints.GetAll(context.LocalFiles);

    [HttpGet("{id:int}")]
    [SwaggerResponse(StatusCodes.Status200OK, type: typeof(LocalFile))]
    [SwaggerResponse(StatusCodes.Status404NotFound)]
    public Results<Ok<LocalFile>, NotFound> Get([FromRoute] int id)
        => EntityEndpoints.GetById(context.LocalFiles, e => e.Id == id);

    [HttpPut("ProcessFile/{id:int}")]
    [SwaggerResponse(StatusCodes.Status200OK)]
    public Ok ProcessFile([FromRoute] int id)
    {
        commandService.Dispatch(new ProcessArgs(id, IdTypeLocalOrFile.LocalId));
        return TypedResults.Ok();
    }
}
