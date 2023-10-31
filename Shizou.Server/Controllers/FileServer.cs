using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Shizou.Data.Database;
using Swashbuckle.AspNetCore.Annotations;

namespace Shizou.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class FileServer : ControllerBase
{
    private readonly ShizouContext _context;
    private readonly ILogger<FileServer> _logger;

    public FileServer(ILogger<FileServer> logger, ShizouContext context)
    {
        _logger = logger;
        _context = context;
    }


    /// <summary>
    ///     Get file by local Id, can optionally end in arbitrary extension
    /// </summary>
    /// <param name="localFileId"></param>
    /// <returns></returns>
    [HttpGet("{localFileId}")]
    [SwaggerResponse(StatusCodes.Status404NotFound)]
    [SwaggerResponse(StatusCodes.Status416RangeNotSatisfiable)]
    [SwaggerResponse(StatusCodes.Status409Conflict)]
    [SwaggerResponse(StatusCodes.Status206PartialContent, contentTypes: "application/octet-stream")]
    [SwaggerResponse(StatusCodes.Status200OK, contentTypes: "application/octet-stream")]
    public ActionResult Get(string localFileId)
    {
        var id = int.Parse(localFileId.Split('.')[0]);
        var localFile = _context.LocalFiles.Include(e => e.ImportFolder).FirstOrDefault(e => e.Id == id);
        if (localFile is null)
            return NotFound();
        if (localFile.ImportFolder is null)
        {
            _logger.LogWarning("Tried to get local file with no import folder");
            return Conflict("Import folder does not exist");
        }

        var fileInfo = new FileInfo(Path.GetFullPath(Path.Combine(localFile.ImportFolder.Path, localFile.PathTail)));
        if (!fileInfo.Exists)
            return Conflict("Local file path does not exist");
        if (!new FileExtensionContentTypeProvider().TryGetContentType(fileInfo.Name, out var mimeType))
            mimeType = "application/octet-stream";
        var fileStream = new FileStream(fileInfo.FullName, FileMode.Open, FileAccess.Read, FileShare.Read, 1 << 19, FileOptions.Asynchronous);

        return File(fileStream, mimeType, fileInfo.Name, true);
    }

    /// <summary>
    ///     Get embedded ASS subtitle of local file
    /// </summary>
    /// <param name="localFileId"></param>
    /// <param name="subtitleIndex"></param>
    /// <returns></returns>
    [HttpGet("AssSubs/{localFileId:int}/{subtitleIndex:int}")]
    [SwaggerResponse(StatusCodes.Status404NotFound)]
    [SwaggerResponse(StatusCodes.Status409Conflict)]
    [SwaggerResponse(StatusCodes.Status200OK, contentTypes: "text/x-ssa")]
    public ActionResult GetAssSubtitle(int localFileId, int subtitleIndex)
    {
        var localFile = _context.LocalFiles.Include(e => e.ImportFolder).FirstOrDefault(e => e.Id == localFileId);
        if (localFile is null)
            return NotFound();
        if (localFile.ImportFolder is null)
        {
            _logger.LogWarning("Tried to get local file with no import folder");
            return Conflict("Import folder does not exist");
        }

        var fileInfo = new FileInfo(Path.GetFullPath(Path.Combine(localFile.ImportFolder.Path, localFile.PathTail)));
        if (!fileInfo.Exists)
            return Conflict("Local file path does not exist");

        using var p = new Process();
        p.StartInfo.UseShellExecute = false;
        p.StartInfo.CreateNoWindow = true;
        p.StartInfo.RedirectStandardOutput = true;
        p.StartInfo.FileName = "ffmpeg";
        p.StartInfo.Arguments = $"-hide_banner -v fatal -i \"{fileInfo.FullName}\" -map 0:{subtitleIndex} -c copy -f ass -";
        p.Start();
        using var memoryStream = new MemoryStream();
        p.StandardOutput.BaseStream.CopyTo(memoryStream);
        var result = memoryStream.ToArray();

        return new FileContentResult(result, "text/x-ssa");
    }

    [HttpGet("{localFileId:int}/play")]
    [Produces("text/html")]
    [SwaggerResponse(StatusCodes.Status200OK)]
    [SwaggerResponse(StatusCodes.Status404NotFound)]
    public ActionResult BrowserPlay(int localFileId)
    {
        _logger.LogInformation("Playing file {LocalFileId} in browser", localFileId);
        var localDbFile = _context.LocalFiles.Include(e => e.ImportFolder).FirstOrDefault(e => e.Id == localFileId);
        if (localDbFile is null)
            return NotFound();
        return Content(@$"<!DOCTYPE html><html><body>
<video controls
src=""{HttpContext.Request.GetEncodedUrl().Remove(HttpContext.Request.GetEncodedUrl().IndexOf("/play", StringComparison.Ordinal))}""></video>
</body></html>
", "text/html");
    }
}
