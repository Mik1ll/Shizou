using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Formatter;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.AspNetCore.OData.Results;
using Microsoft.AspNetCore.OData.Routing.Controllers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Shizou.Database;
using Shizou.Models;

namespace Shizou.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class EntityController<TEntity> : ODataController
        where TEntity : class, IEntity
    {
        private readonly DbSet<TEntity> _dbSet;
        protected readonly ShizouContext Context;
        protected readonly ILogger<EntityController<TEntity>> Logger;

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
        [EnableQuery]
        public virtual ActionResult<IQueryable<TEntity>> Get()
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
        [HttpGet("{key}")]
        [EnableQuery]
        public virtual ActionResult<SingleResult<TEntity>> Get([FromODataUri] int key)
        {
            var result = _dbSet.Where(e => e.Id == key);
            return Ok(SingleResult.Create(result));
        }

        /// <summary>
        ///     Creates new entity
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        /// <response code="201">Entity created</response>
        /// <response code="409">Conflict when trying to add in database</response>
        [HttpPost]
        [EnableQuery]
        public virtual async Task<ActionResult<TEntity>> Post([FromBody] TEntity entity)
        {
            // TODO: Test adding already exiting record
            if (!ModelState.IsValid)
                return BadRequest(ModelState);
            try
            {
                _dbSet.Add(entity);
                await Context.SaveChangesAsync();
            }
            catch (DbUpdateException ex)
            {
                ModelState.AddModelError(string.Empty, ex.InnerException?.Message ?? ex.Message);
                return Conflict(ModelState);
            }
            return CreatedAtAction(nameof(Get), new { id = entity.Id }, entity);
        }

        /// <summary>
        ///     Updates existing entity
        /// </summary>
        /// <param name="key"></param>
        /// <param name="entity"></param>
        /// <returns></returns>
        /// <response code="204">Entity updated</response>
        /// <response code="404">Entity does not exist</response>
        /// <response code="409">Conflict when trying to update in database</response>
        [HttpPut("{key}")]
        [EnableQuery]
        public virtual async Task<ActionResult> Put([FromODataUri] int key, [FromBody] TEntity entity)
        {
            if (entity.Id == 0)
                entity.Id = key;
            if (key == 0 || key != entity.Id)
                return BadRequest("Url key cannot be 0 or mismatch entity id");
            if (!ModelState.IsValid)
                return BadRequest(ModelState);
            Context.Entry(entity).State = EntityState.Modified;
            try
            {
                await Context.SaveChangesAsync();
            }
            catch (DbUpdateException ex)
            {
                if (!Exists(key))
                    return NotFound();
                ModelState.AddModelError(string.Empty, ex.InnerException?.Message ?? ex.Message);
                return Conflict(ModelState);
            }
            return NoContent();
        }

        /// <summary>
        ///     Deletes entity if it exists.
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        /// <response code="204">Entity deleted</response>
        /// <response code="409">Conflict when trying to delete in database</response>
        [HttpDelete("{key}")]
        [EnableQuery]
        public virtual async Task<ActionResult> Delete([FromODataUri] int key)
        {
            var entity = await _dbSet.FindAsync(key);
            if (entity is null)
                return NotFound();
            try
            {
                _dbSet.Remove(entity);
                await Context.SaveChangesAsync();
            }
            catch (DbUpdateException ex)
            {
                ModelState.AddModelError(string.Empty, ex.InnerException?.Message ?? ex.Message);
                return Conflict(ModelState);
            }
            return NoContent();
        }

        protected bool Exists(int key)
        {
            return _dbSet.Any(e => e.Id == key);
        }
    }
}
