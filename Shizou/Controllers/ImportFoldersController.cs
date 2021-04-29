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
        ///     Get import folder by location
        /// </summary>
        /// <param name="location"></param>
        /// <returns></returns>
        [HttpGet("location/{location}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public ActionResult<ImportFolder> GetByLocation(string location)
        {
            return Ok(Context.ImportFolders.Where(f => f.Location == location));
        }
    }
}
