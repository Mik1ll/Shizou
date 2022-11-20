﻿using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Shizou.Database;
using Shizou.Extensions;
using Shizou.Models;

namespace Shizou.Controllers
{
    public class ImportFoldersController : EntityController<ImportFolder>
    {
        public ImportFoldersController(ILogger<ImportFoldersController> logger, ShizouContext context) : base(logger, context)
        {
        }

        /// <summary>
        ///     Get import folder by path
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        [HttpGet("path/{path}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public ActionResult<ImportFolder> GetByPath(string path)
        {
            var importFolder = Context.ImportFolders.GetByPath(path);
            if (importFolder is null)
                return NotFound();
            return Ok(importFolder);
        }
    }
}
