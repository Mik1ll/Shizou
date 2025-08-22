using System.IO;
using System.Net.Mime;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Shizou.Data;
using Shizou.Server.Services;
using Swashbuckle.AspNetCore.Annotations;

namespace Shizou.Server.Controllers;

[ApiController]
[Route($"{Constants.ApiPrefix}/[controller]")]
public class Logs : ControllerBase
{
    private readonly LogFileService _logFileService;

    public Logs(LogFileService logFileService) => _logFileService = logFileService;

    [HttpGet("CurrentFile")]
    [SwaggerResponse(StatusCodes.Status200OK, null, typeof(Stream), MediaTypeNames.Text.Plain)]
    [SwaggerResponse(StatusCodes.Status404NotFound)]
    public async Task<Results<PhysicalFileHttpResult, NotFound>> GetCurrentFile()
    {
        var currentFile = _logFileService.CurrentFile;
        if (currentFile?.Exists is not true)
            return TypedResults.NotFound();
        var tempFile = new FileInfo(Path.GetTempFileName());
        var iFi = currentFile.Open(FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        await using var _ = iFi.ConfigureAwait(false);
        var oFi = tempFile.OpenWrite();
        await using var __ = oFi.ConfigureAwait(false);
        await iFi.CopyToAsync(oFi).ConfigureAwait(false);
        return TypedResults.PhysicalFile(tempFile.FullName, MediaTypeNames.Text.Plain, currentFile.Name, currentFile.LastWriteTimeUtc);
    }
}
