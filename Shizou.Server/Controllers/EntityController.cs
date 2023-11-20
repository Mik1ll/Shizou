using System;
using System.Linq;
using System.Linq.Expressions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Shizou.Data.Database;
using Swashbuckle.AspNetCore.Annotations;

namespace Shizou.Server.Controllers;

public abstract class EntityController<TEntity> : EntityGetController<TEntity> where TEntity : class
{
    protected EntityController(ILogger<EntityController<TEntity>> logger, IShizouContext context, Expression<Func<TEntity, int>> selector) : base(logger,
        context,
        selector)
    {
    }

    /// <summary>
    ///     Creates new entity
    /// </summary>
    /// <param name="entity"></param>
    /// <returns></returns>
    /// <response code="201">Entity created</response>
    /// <response code="400">Bad Request</response>
    /// <response code="409">Conflict when trying to add in database</response>
    [HttpPost]
    [SwaggerResponse(StatusCodes.Status201Created)]
    [SwaggerResponse(StatusCodes.Status400BadRequest)]
    [SwaggerResponse(StatusCodes.Status409Conflict)]
    [Produces("application/json")]
    [Consumes("application/json")]
    public virtual ActionResult<TEntity> Post([FromBody] TEntity entity)
    {
        // TODO: Test adding already existing record
        // TODO: Test adding with child navigation id already existing
        var newEntity = DbSet.Add(entity).Entity;
        try
        {
            Context.SaveChanges();
        }
        catch (DbUpdateException)
        {
            if (Exists(Selector.Compile()(entity)))
                return Conflict();
            throw;
        }

        return CreatedAtAction(nameof(Get), new { id = Selector.Compile()(newEntity) }, newEntity);
    }

    /// <summary>
    ///     Updates existing entity
    /// </summary>
    /// <param name="entity"></param>
    /// <returns></returns>
    /// <response code="204">Entity updated</response>
    /// <response code="404">Entity does not exist</response>
    /// <response code="409">Conflict when trying to update in database</response>
    [HttpPut]
    [SwaggerResponse(StatusCodes.Status204NoContent)]
    [SwaggerResponse(StatusCodes.Status404NotFound)]
    [SwaggerResponse(StatusCodes.Status409Conflict)]
    [SwaggerResponse(StatusCodes.Status400BadRequest, type: typeof(string))]
    [Consumes("application/json")]
    public virtual Results<NoContent, NotFound, Conflict, BadRequest<string>> Put([FromBody] TEntity entity)
    {
        var id = Selector.Compile()(entity);
        if (id == 0)
            return TypedResults.BadRequest("Entity id cannot be 0");
        Context.Entry(entity).State = EntityState.Modified;
        try
        {
            // TODO: Test changing navigation id fields
            Context.SaveChanges();
        }
        catch (DbUpdateException)
        {
            if (!Exists(id))
                return TypedResults.NotFound();
            throw;
        }

        return TypedResults.NoContent();
    }

    /// <summary>
    ///     Deletes entity if it exists.
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    /// <response code="204">Entity deleted</response>
    /// <response code="404">Not Found</response>
    [HttpDelete("{id}")]
    [SwaggerResponse(StatusCodes.Status204NoContent)]
    [SwaggerResponse(StatusCodes.Status404NotFound)]
    public virtual Results<NoContent, NotFound> Delete(int id)
    {
        var entity = DbSet.FirstOrDefault(KeyEqualsExpression(id));
        if (entity is null)
            return TypedResults.NotFound();
        DbSet.Remove(entity);
        Context.SaveChanges();
        return TypedResults.NoContent();
    }

    protected bool Exists(int id)
    {
        return DbSet.Any(KeyEqualsExpression(id));
    }
}
