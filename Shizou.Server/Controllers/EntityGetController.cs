using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Shizou.Data.Database;
using Swashbuckle.AspNetCore.Annotations;

namespace Shizou.Server.Controllers;

public abstract class EntityGetController<TEntity> : ControllerBase where TEntity : class
{
    protected readonly IShizouContext Context;
    protected readonly Expression<Func<TEntity, int>> Selector;
    protected readonly IShizouDbSet<TEntity> DbSet;

    protected EntityGetController(IShizouContext context, Expression<Func<TEntity, int>> selector)
    {
        Context = context;
        Selector = selector;
        DbSet = Context.Set<TEntity>();
    }

    [HttpGet]
    [SwaggerResponse(StatusCodes.Status200OK)]
    public ActionResult<List<TEntity>> Get()
    {
        return Ok(DbSet.AsNoTracking().ToList());
    }

    [HttpGet("{id:int}")]
    [SwaggerResponse(StatusCodes.Status200OK)]
    [SwaggerResponse(StatusCodes.Status404NotFound)]
    public ActionResult<TEntity> Get([FromRoute] int id)
    {
        var exp = KeyEqualsExpression(id);
        var result = DbSet.AsNoTracking().SingleOrDefault(exp);
        if (result is null)
            return NotFound();
        return Ok(result);
    }

    protected Expression<Func<TEntity, bool>> KeyEqualsExpression(int id)
    {
        return Expression.Lambda<Func<TEntity, bool>>(Expression.Equal(Selector.Body, Expression.Constant(id)), Selector.Parameters);
    }
}
