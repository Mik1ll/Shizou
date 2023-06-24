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
[Route("[controller]")]
public class EntityController<TEntity> : ControllerBase
    where TEntity : class, IEntity
{
    protected readonly ShizouContext Context;
    protected readonly ILogger<EntityController<TEntity>> Logger;
    private readonly DbSet<TEntity> _dbSet;

    public EntityController(ILogger<EntityController<TEntity>> logger, ShizouContext context)
    {
        Logger = logger;
        Context = context;
        _dbSet = Context.Set<TEntity>();
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
        return _dbSet.AsNoTracking().ToList();
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
        var result = _dbSet.Find(id);
        if (result is null)
            return NotFound();
        return result;
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
        var newEntity = _dbSet.Add(entity).Entity;
        try
        {
            Context.SaveChanges();
        }
        catch (DbUpdateException)
        {
            if (Exists(entity.Id))
                return Conflict();
            throw;
        }
        return CreatedAtAction(nameof(Get), new { id = newEntity.Id }, newEntity);
    }

    /// <summary>
    ///     Updates existing entity
    /// </summary>
    /// <param name="id"></param>
    /// <param name="entity"></param>
    /// <returns></returns>
    /// <response code="204">Entity updated</response>
    /// <response code="404">Entity does not exist</response>
    /// <response code="409">Conflict when trying to update in database</response>
    [HttpPut("{id}")]
    [SwaggerResponse(StatusCodes.Status204NoContent)]
    [SwaggerResponse(StatusCodes.Status404NotFound)]
    [SwaggerResponse(StatusCodes.Status409Conflict)]
    [Consumes("application/json")]
    public virtual ActionResult Put(int id, [FromBody] TEntity entity)
    {
        if (entity.Id == 0)
            entity.Id = id;
        if (id == 0 || id != entity.Id)
            return BadRequest("Url id cannot be 0 or mismatch entity id");
        Context.Entry(entity).State = EntityState.Modified;
        try
        {
            // TODO: Test changing navigation id fields
            Context.SaveChanges();
        }
        catch (DbUpdateException)
        {
            if (!Exists(id))
                return NotFound();
            throw;
        }
        return NoContent();
    }

    /// <summary>
    ///     Deletes entity if it exists.
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    /// <response code="204">Entity deleted</response>
    /// <response code="404">Not Found</response>
    /// <response code="409">Conflict when trying to delete in database</response>
    [HttpDelete("{id}")]
    [SwaggerResponse(StatusCodes.Status204NoContent)]
    [SwaggerResponse(StatusCodes.Status404NotFound)]
    [SwaggerResponse(StatusCodes.Status409Conflict)]
    public virtual ActionResult Delete(int id)
    {
        var entity = _dbSet.Find(id);
        if (entity is null)
            return NotFound();
        _dbSet.Remove(entity);
        Context.SaveChanges();
        return NoContent();
    }

    protected bool Exists(int id)
    {
        return _dbSet.Any(e => e.Id == id);
    }
}
