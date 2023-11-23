using System;
using System.Linq;
using System.Linq.Expressions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Shizou.Data.Database;
using Swashbuckle.AspNetCore.Annotations;

namespace Shizou.Server.Controllers;

public abstract class EntityController<TEntity> : EntityGetController<TEntity> where TEntity : class
{
    protected EntityController(IShizouContext context, Expression<Func<TEntity, int>> selector) : base(context,
        selector)
    {
    }

    [HttpPost]
    [SwaggerResponse(StatusCodes.Status201Created)]
    [SwaggerResponse(StatusCodes.Status400BadRequest, type: typeof(ProblemDetails))]
    [SwaggerResponse(StatusCodes.Status409Conflict)]
    [Produces("application/json")]
    [Consumes("application/json")]
    public ActionResult<TEntity> Post([FromBody] TEntity entity)
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

    [HttpPut]
    [SwaggerResponse(StatusCodes.Status204NoContent, "Entity updated")]
    [SwaggerResponse(StatusCodes.Status404NotFound)]
    [SwaggerResponse(StatusCodes.Status400BadRequest, type: typeof(string))]
    [SwaggerResponse(StatusCodes.Status400BadRequest, type: typeof(ProblemDetails))]
    [Consumes("application/json")]
    public Results<NoContent, NotFound, BadRequest<string>, BadRequest<ProblemDetails>> Put([FromBody] TEntity entity)
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

    [HttpDelete("{id}")]
    [SwaggerResponse(StatusCodes.Status204NoContent, "Entity deleted")]
    [SwaggerResponse(StatusCodes.Status404NotFound)]
    [SwaggerResponse(StatusCodes.Status400BadRequest, type: typeof(ProblemDetails))]
    public Results<NoContent, NotFound, BadRequest<ProblemDetails>> Delete(int id)
    {
        var entity = DbSet.FirstOrDefault(KeyEqualsExpression(id));
        if (entity is null)
            return TypedResults.NotFound();
        DbSet.Remove(entity);
        Context.SaveChanges();
        return TypedResults.NoContent();
    }

    private bool Exists(int id)
    {
        return DbSet.Any(KeyEqualsExpression(id));
    }
}
