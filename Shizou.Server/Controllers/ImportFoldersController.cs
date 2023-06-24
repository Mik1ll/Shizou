using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Shizou.Data.Database;
using Shizou.Data.Models;
using Shizou.Server.Extensions;
using Swashbuckle.AspNetCore.Annotations;

namespace Shizou.Server.Controllers;

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
    [SwaggerResponse(StatusCodes.Status200OK)]
    [SwaggerResponse(StatusCodes.Status404NotFound)]
    [Produces("application/json")]
    public ActionResult<ImportFolder> GetByPath(string path)
    {
        var importFolder = Context.ImportFolders.GetByPath(path);
        if (importFolder is null)
            return NotFound();
        return importFolder;
    }
}
