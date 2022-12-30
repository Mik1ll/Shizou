using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Shizou.Services;

namespace Shizou.Controllers;

[ApiController]
[Route("[controller]")]
public class ImportController : ControllerBase
{
    private readonly ImportService _importService;

    public ImportController(ImportService importService)
    {
        _importService = importService;
    }

    /// <summary>
    ///     Start import
    /// </summary>
    /// <returns></returns>
    [HttpPut("start")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public ActionResult StartImport()
    {
        _importService.Import();
        return Ok();
    }

    /// <summary>
    ///     Scan an import folder
    /// </summary>
    /// <param name="folderId"></param>
    /// <returns></returns>
    [HttpPut("scan/{folderId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public ActionResult ScanFolder(int folderId)
    {
        _importService.ScanImportFolder(folderId);
        return Ok();
    }
}