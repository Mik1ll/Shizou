using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Shizou.Database;
using Shizou.Entities;

namespace Shizou.Controllers
{
    public class ImportFoldersController : EntityController<ImportFolder>
    {
        public ImportFoldersController(ILogger<EntityController<ImportFolder>> logger, ShizouContext context) : base(logger, context)
        {
        }

        /// <summary>
        ///     Get import folder by path
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        [HttpGet("path/{path}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public ActionResult<ImportFolder> GetByPath(string path)
        {
            return Ok(Context.ImportFolders.Where(f => f.Path == path));
        }
    }
}
