using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Shizou.Data;
using Shizou.Server.Services;
using Swashbuckle.AspNetCore.Annotations;

namespace Shizou.Server.Controllers;

[ApiController]
[Route($"{Constants.ApiPrefix}/[controller]")]
public class Import : ControllerBase
{
    private readonly ImportService _importService;

    public Import(ImportService importService)
    {
        _importService = importService;
    }

    /// <summary>
    ///     Start import
    /// </summary>
    /// <returns></returns>
    [HttpPut("[action]")]
    [SwaggerResponse(StatusCodes.Status200OK)]
    public Ok StartImport()
    {
        _importService.Import();
        return TypedResults.Ok();
    }

    /// <summary>
    ///     Scan an import folder
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    [HttpPut("[action]/{id:int}")]
    [SwaggerResponse(StatusCodes.Status200OK)]
    public Ok ScanFolder([FromRoute] int id)
    {
        _importService.ScanImportFolder(id);
        return TypedResults.Ok();
    }
}
