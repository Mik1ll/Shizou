using System.Linq;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Shizou.Database;
using Shizou.Dtos;
using Shizou.Entities;

namespace Shizou.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class EntityController<TDto, TEntity> : ControllerBase
        where TDto : EntityDto, new()
        where TEntity : Entity, new()
    {
        private readonly DbSet<TEntity> _dbSet;
        protected readonly ShizouContext Context;
        protected readonly IMapper _mapper;
        protected readonly ILogger<EntityController<TDto, TEntity>> Logger;

        public EntityController(ILogger<EntityController<TDto, TEntity>> logger, ShizouContext context, IMapper mapper)
        {
            Logger = logger;
            Context = context;
            _mapper = mapper;
            _dbSet = Context.Set<TEntity>();
        }

        /// <summary>
        ///     Get all entities
        /// </summary>
        /// <returns></returns>
        /// <response code="200">Success</response>
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [Produces("application/json")]
        public virtual ActionResult<IQueryable<TDto>> List()
        {
            return Ok(_dbSet.AsNoTracking().ProjectTo<TDto>(_mapper.ConfigurationProvider));
        }

        /// <summary>
        ///     Get entity
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        /// <response code="404">Entity is not found</response>
        /// <response code="200">Entity found</response>
        [HttpGet("{id:int}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [Produces("application/json")]
        public virtual ActionResult<TDto> Get(int id)
        {
            var result = _dbSet.AsNoTracking().Where(e => e.Id == id).ProjectTo<TDto>(_mapper.ConfigurationProvider).SingleOrDefault();
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
        [Produces("application/json")]
        public virtual ActionResult<TDto> Create([FromBody] TDto entity)
        {
            try
            {
                var newEntity = _mapper.Map<TEntity>(entity);
                _dbSet.Add(newEntity);
                Context.SaveChanges();
                return CreatedAtAction(nameof(Get), new { id = newEntity.Id }, _mapper.Map<TDto>(newEntity));
            }
            catch (DbUpdateException ex)
            {
                ModelState.AddModelError(string.Empty, ex.InnerException?.Message ?? ex.Message);
                return Conflict(ModelState);
            }
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
        public virtual ActionResult Update([FromBody] TDto entity)
        {
            try
            {
                if (!_dbSet.Any(e => e.Id == entity.Id))
                    return NotFound();
                _dbSet.Update(_mapper.Map<TEntity>(entity));
                Context.SaveChanges();
            }
            catch (DbUpdateException ex)
            {
                ModelState.AddModelError(string.Empty, ex.InnerException?.Message ?? ex.Message);
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
                    ModelState.AddModelError(string.Empty, ex.InnerException?.Message ?? ex.Message);
                    return Conflict(ModelState);
                }
            return NoContent();
        }
    }
}
