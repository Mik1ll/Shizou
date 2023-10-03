using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Shizou.Data.Database;
using Swashbuckle.AspNetCore.Annotations;

namespace Shizou.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class EntityGetController<TEntity> : ControllerBase where TEntity : class
{
    protected readonly ShizouContext Context;
    protected readonly Expression<Func<TEntity, int>> Selector;
    protected readonly ILogger<EntityGetController<TEntity>> Logger;
    protected readonly DbSet<TEntity> DbSet;

    public EntityGetController(ILogger<EntityGetController<TEntity>> logger, ShizouContext context, Expression<Func<TEntity, int>> selector)
    {
        Logger = logger;
        Context = context;
        Selector = selector;
        DbSet = Context.Set<TEntity>();
    }

    /// <summary>
    ///     Get all entities
    /// </summary>
    /// <returns></returns>
    /// <response code="200">Success</response>
    [HttpGet]
    [SwaggerResponse(StatusCodes.Status200OK)]
    [Produces("application/json")]
    public virtual ActionResult<List<TEntity>> Get()
    {
        return DbSet.AsNoTracking().ToList();
    }

    /// <summary>
    ///     Get entity
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    /// <response code="404">Entity is not found</response>
    /// <response code="200">Entity found</response>
    [HttpGet("{id}")]
    [SwaggerResponse(StatusCodes.Status200OK)]
    [SwaggerResponse(StatusCodes.Status404NotFound)]
    [Produces("application/json")]
    public virtual ActionResult<TEntity> Get(int id)
    {
        var exp = KeyEqualsExpression(id);
        var result = DbSet.AsNoTracking().SingleOrDefault(exp);
        if (result is null)
            return NotFound();
        return result;
    }

    protected Expression<Func<TEntity, bool>> KeyEqualsExpression(int id)
    {
        return Expression.Lambda<Func<TEntity, bool>>(Expression.Equal(Selector.Body, Expression.Constant(id)), Selector.Parameters);
    }
}
