using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Http.HttpResults;
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
    public Results<FileStreamHttpResult, Conflict<string>, NotFound> Get(string localFileId)
    {
        var id = int.Parse(localFileId.Split('.')[0]);
        var localFile = _context.LocalFiles.Include(e => e.ImportFolder).FirstOrDefault(e => e.Id == id);
        if (localFile is null)
            return TypedResults.NotFound();
        if (localFile.ImportFolder is null)
        {
            _logger.LogWarning("Tried to get local file with no import folder");
            return TypedResults.Conflict("Import folder does not exist");
        }

        var fileInfo = new FileInfo(Path.GetFullPath(Path.Combine(localFile.ImportFolder.Path, localFile.PathTail)));
        if (!fileInfo.Exists)
            return TypedResults.Conflict("Local file path does not exist");
        if (!new FileExtensionContentTypeProvider().TryGetContentType(fileInfo.Name, out var mimeType))
            mimeType = "application/octet-stream";
        var fileStream = new FileStream(fileInfo.FullName, FileMode.Open, FileAccess.Read, FileShare.Read, 1 << 19, FileOptions.Asynchronous);
        return TypedResults.File(fileStream, mimeType, fileInfo.Name, enableRangeProcessing: true);
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
    public async Task<Results<FileContentHttpResult, NotFound, Conflict<string>>> GetAssSubtitle(int localFileId, int subtitleIndex)
    {
        var localFile = _context.LocalFiles.Include(e => e.ImportFolder).FirstOrDefault(e => e.Id == localFileId);
        if (localFile is null)
            return TypedResults.NotFound();
        if (localFile.ImportFolder is null)
        {
            _logger.LogWarning("Tried to get local file with no import folder");
            return TypedResults.Conflict("Import folder does not exist");
        }

        var fileInfo = new FileInfo(Path.GetFullPath(Path.Combine(localFile.ImportFolder.Path, localFile.PathTail)));
        if (!fileInfo.Exists)
            return TypedResults.Conflict("Local file path does not exist");

        using var p = new Process();
        p.StartInfo.UseShellExecute = false;
        p.StartInfo.CreateNoWindow = true;
        p.StartInfo.RedirectStandardOutput = true;
        p.StartInfo.FileName = "ffmpeg";
        p.StartInfo.Arguments = $"-hide_banner -v fatal -i \"{fileInfo.FullName}\" -map 0:{subtitleIndex} -c ass -f ass -";
        p.Start();
        using var memoryStream = new MemoryStream();
        await p.StandardOutput.BaseStream.CopyToAsync(memoryStream);
        var result = memoryStream.ToArray();
        return TypedResults.File(result, "text/x-ssa");
    }

    /// <summary>
    ///     Get embedded ASS subtitle of local file
    /// </summary>
    /// <param name="localFileId"></param>
    /// <param name="fontIndex"></param>
    /// <param name="fontName"></param>
    /// <returns></returns>
    [HttpGet("Fonts/{localFileId:int}/{fontIndex:int}")]
    [SwaggerResponse(StatusCodes.Status404NotFound)]
    [SwaggerResponse(StatusCodes.Status400BadRequest, type: typeof(string))]
    [SwaggerResponse(StatusCodes.Status409Conflict, type: typeof(string))]
    [SwaggerResponse(StatusCodes.Status200OK, contentTypes: new[] { "font/ttf", "font/otf" })]
    public async Task<Results<PhysicalFileHttpResult, BadRequest<string>, Conflict<string>, NotFound>> GetFont(int localFileId, int fontIndex,
        [FromQuery] string? fontName)
    {
        var localFile = _context.LocalFiles.Include(e => e.ImportFolder).FirstOrDefault(e => e.Id == localFileId);
        if (localFile is null)
            return TypedResults.NotFound();
        if (localFile.ImportFolder is null)
        {
            _logger.LogWarning("Tried to get local file with no import folder");
            return TypedResults.Conflict("Import folder does not exist");
        }

        if (fontName is null)
        {
            _logger.LogWarning("Tried to get font without font name");
            return TypedResults.BadRequest("No font name provided");
        }

        var fileInfo = new FileInfo(Path.GetFullPath(Path.Combine(localFile.ImportFolder.Path, localFile.PathTail)));
        if (!fileInfo.Exists)
            return TypedResults.Conflict("Local file path does not exist");

        var tempDirectory = Path.Combine(Path.GetTempPath(), "ShizouFonts", localFileId.ToString());
        var fontPath = Path.Combine(tempDirectory, fontName);
        if (!Path.Exists(fontPath))
        {
            Directory.CreateDirectory(tempDirectory);
            using var p = new Process();
            p.StartInfo.UseShellExecute = false;
            p.StartInfo.CreateNoWindow = true;
            p.StartInfo.RedirectStandardOutput = true;
            p.StartInfo.FileName = "ffmpeg";
            p.StartInfo.Arguments = $"-hide_banner -v fatal -y -dump_attachment:{fontIndex} \"\" -i \"{fileInfo.FullName}\"";
            p.StartInfo.WorkingDirectory = tempDirectory;
            p.Start();
            await p.WaitForExitAsync();
        }

        if (!new FileExtensionContentTypeProvider().TryGetContentType(fontName, out var mimeType))
            mimeType = "font/ttf";
        return TypedResults.PhysicalFile(fontPath, mimeType);
    }

    [HttpGet("{localFileId:int}/play")]
    [Produces("text/html")]
    [SwaggerResponse(StatusCodes.Status200OK)]
    [SwaggerResponse(StatusCodes.Status404NotFound)]
    public Results<ContentHttpResult, NotFound> BrowserPlay(int localFileId)
    {
        _logger.LogInformation("Playing file {LocalFileId} in browser", localFileId);
        var localDbFile = _context.LocalFiles.Include(e => e.ImportFolder).FirstOrDefault(e => e.Id == localFileId);
        if (localDbFile is null)
            return TypedResults.NotFound();
        return TypedResults.Content(@$"<!DOCTYPE html><html><body>
<video controls
src=""{HttpContext.Request.GetEncodedUrl().Remove(HttpContext.Request.GetEncodedUrl().IndexOf("/play", StringComparison.Ordinal))}""></video>
</body></html>
", "text/html");
    }
}
