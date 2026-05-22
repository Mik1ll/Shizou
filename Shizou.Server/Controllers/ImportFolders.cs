using System.Collections.Generic;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Shizou.Data;
using Shizou.Data.Database;
using Shizou.Data.Models;
using Shizou.Server.Extensions.Query;
using Swashbuckle.AspNetCore.Annotations;

namespace Shizou.Server.Controllers;

[ApiController]
[Route($"{Constants.ApiPrefix}/[controller]")]
public class ImportFolders(IShizouContext context) : ControllerBase
{
    [HttpGet]
    [SwaggerResponse(StatusCodes.Status200OK, type: typeof(List<ImportFolder>))]
    public Ok<List<ImportFolder>> GetAll() => EntityEndpoints.GetAll(context.ImportFolders);

    [HttpGet("{id:int}")]
    [SwaggerResponse(StatusCodes.Status200OK, type: typeof(ImportFolder))]
    [SwaggerResponse(StatusCodes.Status404NotFound)]
    public Results<Ok<ImportFolder>, NotFound> Get([FromRoute] int id)
        => EntityEndpoints.GetById(context.ImportFolders, e => e.Id == id);

    [HttpPost]
    [SwaggerResponse(StatusCodes.Status201Created, type: typeof(ImportFolder))]
    [SwaggerResponse(StatusCodes.Status400BadRequest)]
    [SwaggerResponse(StatusCodes.Status409Conflict)]
    public Results<Created<ImportFolder>, Conflict> Post([FromBody] ImportFolder entity)
        => EntityEndpoints.Create(context.ImportFolders, context, entity,
            e => e.Id == entity.Id,
            newEntity => Url.Action(nameof(Get), new { id = newEntity.Id }));

    [HttpPut]
    [SwaggerResponse(StatusCodes.Status204NoContent)]
    [SwaggerResponse(StatusCodes.Status404NotFound)]
    [SwaggerResponse(StatusCodes.Status400BadRequest)]
    public Results<NoContent, NotFound, ProblemHttpResult> Put([FromBody] ImportFolder entity)
        => EntityEndpoints.Update(context.ImportFolders, context, entity,
            entity.Id, e => e.Id == entity.Id);

    [HttpDelete("{id:int}")]
    [SwaggerResponse(StatusCodes.Status204NoContent, "Entity deleted")]
    [SwaggerResponse(StatusCodes.Status404NotFound)]
    public Results<NoContent, NotFound> Delete(int id)
        => EntityEndpoints.Remove(context.ImportFolders, context, e => e.Id == id);

    /// <summary>
    ///     Get import folder by path
    /// </summary>
    /// <param name="path"></param>
    /// <returns></returns>
    [HttpGet("path/{path}")]
    [SwaggerResponse(StatusCodes.Status200OK, type: typeof(ImportFolder))]
    [SwaggerResponse(StatusCodes.Status404NotFound)]
    public Results<Ok<ImportFolder>, NotFound> GetByPath([FromRoute] string path)
    {
        var importFolder = context.ImportFolders.ByPath(path);
        if (importFolder is null)
            return TypedResults.NotFound();
        return TypedResults.Ok(importFolder);
    }
}
