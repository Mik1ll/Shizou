﻿using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Shizou.Database;
using Shizou.Entities;

namespace Shizou.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class ImportFolderController : ControllerBase
    {
        private readonly ShizouContext _context;
        private readonly ILogger<ImportFolderController> _logger;

        public ImportFolderController(ILogger<ImportFolderController> logger, ShizouContext context)
        {
            _logger = logger;
            _context = context;
        }

        /// <summary>
        ///     Get all import folders
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public ActionResult<IEnumerable<ImportFolder>> GetAll()
        {
            return Ok(_context.ImportFolders.ToList());
        }

        /// <summary>
        ///     Get import folder
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpGet("{id}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public ActionResult<ImportFolder> Get(int id)
        {
            return Ok(_context.ImportFolders.Find(id));
        }

        /// <summary>
        ///     Get import folder by location
        /// </summary>
        /// <param name="location"></param>
        /// <returns></returns>
        [HttpGet("location/{location}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public ActionResult<ImportFolder> GetByLocation(string location)
        {
            return Ok(_context.ImportFolders.Where(f => f.Location == location));
        }

        /// <summary>
        ///     Create or updates folder if it exists.
        /// </summary>
        /// <param name="importFolder">id 0 or none if inserting</param>
        /// <returns></returns>
        /// <response code="201">Folder is new</response>
        /// <response code="204">Folder updated</response>
        /// <response code="404">Folder not found</response>
        [HttpPost]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public ActionResult Save([FromBody] ImportFolder importFolder)
        {
            ActionResult response;
            try
            {
                var oldid = importFolder.Id;
                _context.ImportFolders.Update(importFolder);
                _context.SaveChanges();
                var path = new Uri(@$"{Request.Scheme}://{Request.Host.ToUriComponent()}{Request.Path}/{importFolder.Id}");
                response = oldid != 0 ? NoContent() : Created(path, null);
            }
            catch (KeyNotFoundException)
            {
                response = NotFound();
            }
            return response;
        }

        /// <summary>
        ///     Deletes folder if it exists.
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        /// <response code="204">Folder deleted</response>
        /// <response code="404">Folder not found</response>
        [HttpDelete]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public ActionResult Delete(int id)
        {
            try
            {
                _context.ImportFolders.Remove(new ImportFolder {Id = id});
                _context.SaveChanges();
                return NoContent();
            }
            catch (KeyNotFoundException)
            {
                return NotFound();
            }
        }
    }
}
