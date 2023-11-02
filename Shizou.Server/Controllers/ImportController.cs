﻿using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Shizou.Server.Services;
using Swashbuckle.AspNetCore.Annotations;

namespace Shizou.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
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
    [SwaggerResponse(StatusCodes.Status200OK)]
    public Ok StartImport()
    {
        _importService.Import();
        return TypedResults.Ok();
    }

    /// <summary>
    ///     Scan an import folder
    /// </summary>
    /// <param name="folderId"></param>
    /// <returns></returns>
    [HttpPut("scan/{folderId}")]
    [SwaggerResponse(StatusCodes.Status200OK)]
    public Ok ScanFolder(int folderId)
    {
        _importService.ScanImportFolder(folderId);
        return TypedResults.Ok();
    }
}
