using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Shizou.Data.Database;
using Shizou.Data.Models;
using Swashbuckle.AspNetCore.Annotations;

namespace Shizou.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class EntityGetController<TEntity> : ControllerBase where TEntity : class, IEntity
{
    protected readonly ShizouContext Context;
    protected readonly ILogger<EntityGetController<TEntity>> Logger;
    protected readonly DbSet<TEntity> DbSet;

    public EntityGetController(ILogger<EntityGetController<TEntity>> logger, ShizouContext context)
    {
        Logger = logger;
        Context = context;
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
        var result = DbSet.AsNoTracking().SingleOrDefault(e => e.Id == id);
        if (result is null)
            return NotFound();
        return result;
    }
}
