using System;
using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Shizou.Database;
using Shizou.Entities;

namespace Shizou.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class EntityController<T> : ControllerBase where T : Entity, new()
    {
        private readonly DbSet<T> _dbSet;
        protected readonly ShizouContext Context;
        protected readonly ILogger<EntityController<T>> Logger;

        public EntityController(ILogger<EntityController<T>> logger, ShizouContext context)
        {
            Logger = logger;
            Context = context;
            _dbSet = Context.Set<T>();
        }

        /// <summary>
        ///     Get all entities
        /// </summary>
        /// <returns></returns>
        /// <response code="200">Success</response>
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public virtual ActionResult<IQueryable<T>> List()
        {
            return Ok(_dbSet);
        }

        /// <summary>
        ///     Get entity
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        /// <response code="404">Entity is not found</response>
        /// <response code="200">Entity found</response>
        [HttpGet("{key:int}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public virtual ActionResult<T> Get(int key)
        {
            var result = _dbSet.Find(key);
            return result is null ? NotFound() : Ok(result);
        }

        /// <summary>
        ///     Creates new entity
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        /// <response code="201">Entity created</response>
        /// <response code="409">Conflict when trying to add in database</response>
        [HttpPost]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public virtual ActionResult<T> Create([FromBody] T entity)
        {
            try
            {
                _dbSet.Add(entity);
                Context.SaveChanges();
            }
            catch (DbUpdateException ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                return Conflict(ModelState);
            }
            var path = new Uri(@$"{Request.Scheme}://{Request.Host.ToUriComponent()}{Request.Path}/{entity.Id}");
            return Created(path, entity);
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
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public virtual ActionResult Update([FromBody] T entity)
        {
            try
            {
                if (!_dbSet.Any(e => e.Id == entity.Id))
                    return NotFound();
                _dbSet.Update(entity);
                Context.SaveChanges();
            }
            catch (DbUpdateException ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                return Conflict(ModelState);
            }
            return NoContent();
        }

        /// <summary>
        ///     Deletes entity if it exists.
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        /// <response code="204">Entity deleted</response>
        /// <response code="409">Conflict when trying to delete in database</response>
        [HttpDelete]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public virtual ActionResult Delete(int id)
        {
            var entity = _dbSet.Find(id);
            if (entity is not null)
                try
                {
                    _dbSet.Remove(entity);
                    Context.SaveChanges();
                }
                catch (DbUpdateException ex)
                {
                    ModelState.AddModelError(string.Empty, ex.Message);
                    return Conflict(ModelState);
                }
            return NoContent();
        }
    }
}
