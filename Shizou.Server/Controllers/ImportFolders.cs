using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Shizou.Data.Database;
using Shizou.Data.Models;
using Shizou.Server.Extensions.Query;
using Swashbuckle.AspNetCore.Annotations;

namespace Shizou.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ImportFolders : EntityController<ImportFolder>
{
    public ImportFolders(IShizouContext context) : base(context, folder => folder.Id)
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
    public Results<Ok<ImportFolder>, NotFound> GetByPath(string path)
    {
        var importFolder = Context.ImportFolders.ByPath(path);
        if (importFolder is null)
            return TypedResults.NotFound();
        return TypedResults.Ok(importFolder);
    }
}
