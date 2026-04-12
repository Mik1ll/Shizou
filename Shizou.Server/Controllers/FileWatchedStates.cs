using System.Collections.Generic;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Shizou.Data;
using Shizou.Data.Database;
using Shizou.Data.Models;
using Swashbuckle.AspNetCore.Annotations;

namespace Shizou.Server.Controllers;

[ApiController]
[Route($"{Constants.ApiPrefix}/[controller]")]
public class FileWatchedStates(IShizouContext context) : ControllerBase
{
    [HttpGet]
    [SwaggerResponse(StatusCodes.Status200OK, type: typeof(List<FileWatchedState>))]
    public Ok<List<FileWatchedState>> GetAll() => EntityEndpoints.GetAll(context.FileWatchedStates);

    [HttpGet("{id:int}")]
    [SwaggerResponse(StatusCodes.Status200OK, type: typeof(FileWatchedState))]
    [SwaggerResponse(StatusCodes.Status404NotFound)]
    public Results<Ok<FileWatchedState>, NotFound> Get([FromRoute] int id)
        => EntityEndpoints.GetById(context.FileWatchedStates, e => e.AniDbFileId == id);

    [HttpPost]
    [SwaggerResponse(StatusCodes.Status201Created, type: typeof(FileWatchedState))]
    [SwaggerResponse(StatusCodes.Status400BadRequest)]
    [SwaggerResponse(StatusCodes.Status409Conflict)]
    public Results<Created<FileWatchedState>, Conflict> Post([FromBody] FileWatchedState entity)
        => EntityEndpoints.Create(context.FileWatchedStates, context, entity,
            e => e.AniDbFileId == entity.AniDbFileId,
            newEntity => Url.Action(nameof(Get), new { id = newEntity.AniDbFileId }));

    [HttpPut]
    [SwaggerResponse(StatusCodes.Status204NoContent)]
    [SwaggerResponse(StatusCodes.Status404NotFound)]
    [SwaggerResponse(StatusCodes.Status400BadRequest)]
    public Results<NoContent, NotFound, ProblemHttpResult> Put([FromBody] FileWatchedState entity)
        => EntityEndpoints.Update(context.FileWatchedStates, context, entity,
            entity.AniDbFileId, e => e.AniDbFileId == entity.AniDbFileId);

    [HttpDelete("{id:int}")]
    [SwaggerResponse(StatusCodes.Status204NoContent, "Entity deleted")]
    [SwaggerResponse(StatusCodes.Status404NotFound)]
    public Results<NoContent, NotFound> Delete(int id)
        => EntityEndpoints.Remove(context.FileWatchedStates, context, e => e.AniDbFileId == id);
}
