﻿using System;
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
            _dbSet = (DbSet<T>)Context.GetType().GetProperties().Single(_ => _.PropertyType == typeof(DbSet<T>)).GetValue(Context)!;
        }

        /// <summary>
        ///     Get all entities
        /// </summary>
        /// <returns></returns>
        /// <response code="200">Success</response>
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [Produces("application/json")]
        public ActionResult<IQueryable<T>> List()
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
        [Produces("application/json")]
        public ActionResult<T> Get(int key)
        {
            var result = _dbSet.Find(key);
            return result is null ? NotFound() : Ok(result);
        }


        [HttpPost]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public ActionResult<T> Create([FromBody] T entity)
        {
            try
            {
                _dbSet.Add(entity);
                Context.SaveChanges();
            }
            catch (DbUpdateException ex)
            {
                return Conflict();
            }
            var path = new Uri(@$"{Request.Scheme}://{Request.Host.ToUriComponent()}{Request.Path}/{entity.Id}");
            return Created(path, entity);
        }

        /// <summary>
        ///     Create or updates entity if it exists.
        /// </summary>
        /// <param name="entity">id 0 if inserting</param>
        /// <returns></returns>
        /// <response code="201">Entity is new</response>
        /// <response code="204">Entity updated</response>
        /// <response code="404">Entity not found</response>
        // [HttpPost]
        // [ProducesResponseType(StatusCodes.Status204NoContent)]
        // [ProducesResponseType(StatusCodes.Status201Created)]
        // [ProducesResponseType(StatusCodes.Status404NotFound)]
        // [EnableQuery]
        // public ActionResult Save([FromBody] T entity)
        // {
        //     ActionResult response;
        //     try
        //     {
        //         var oldid = entity.Id;
        //         _dbSet.Update(entity);
        //         Context.SaveChanges();
        //         var path = new Uri(@$"{Request.Scheme}://{Request.Host.ToUriComponent()}{Request.Path}/{entity.Id}");
        //         response = oldid != 0 ? NoContent() : Created(path, null);
        //     }
        //     catch (KeyNotFoundException)
        //     {
        //         response = NotFound();
        //     }
        //     return response;
        // }

        /// <summary>
        ///     Deletes entity if it exists.
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        /// <response code="204">Entity deleted</response>
        [HttpDelete]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public ActionResult Delete(int id)
        {
            var entity = _dbSet.Find(id);
            if (entity is not null)
            {
                _dbSet.Remove(entity);
                Context.SaveChanges();
            }
            return NoContent();
        }
    }
}
