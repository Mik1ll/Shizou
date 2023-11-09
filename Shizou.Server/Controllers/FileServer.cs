using System;
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
using Shizou.Server.Services;
using Swashbuckle.AspNetCore.Annotations;

namespace Shizou.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class FileServer : ControllerBase
{
    private readonly ShizouContext _context;
    private readonly SubtitleService _subtitleService;
    private readonly ILogger<FileServer> _logger;

    public FileServer(ILogger<FileServer> logger, ShizouContext context, SubtitleService subtitleService)
    {
        _logger = logger;
        _context = context;
        _subtitleService = subtitleService;
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


    public record GetSubtitleArgs(string Ed2K, int Index);

    /// <summary>
    ///     Get embedded ASS subtitle of local file
    /// </summary>
    /// <param name="args"></param>
    /// <returns></returns>
    [HttpGet("Subs/{ed2k}/{index:int}")]
    [SwaggerResponse(StatusCodes.Status404NotFound)]
    [SwaggerResponse(StatusCodes.Status200OK, contentTypes: "text/x-ssa")]
    public async Task<Results<PhysicalFileHttpResult, NotFound>> GetSubtitle([FromRoute] GetSubtitleArgs args)
    {
        // ReSharper disable once InconsistentNaming
        var (ed2k, index) = args;
        var fileInfo = new FileInfo(SubtitleService.GetSubPath(ed2k, index));
        if (!fileInfo.Exists)
        {
            await _subtitleService.ExtractSubtitles(ed2k);
            fileInfo.Refresh();
            if (!fileInfo.Exists)
                return TypedResults.NotFound();
        }

        return TypedResults.PhysicalFile(fileInfo.FullName, "text/x-ssa");
    }

    public record GetFontArgs(string Ed2K, string FontName);

    /// <summary>
    ///     Get embedded font of local file
    /// </summary>
    /// <param name="args"></param>
    /// <returns></returns>
    [HttpGet("Fonts/{ed2k}/{fontName}")]
    [SwaggerResponse(StatusCodes.Status404NotFound)]
    [SwaggerResponse(StatusCodes.Status200OK, contentTypes: new[] { "font/ttf", "font/otf" })]
    public async Task<Results<PhysicalFileHttpResult, NotFound>> GetFont([FromRoute] GetFontArgs args)
    {
        // ReSharper disable once InconsistentNaming
        var (ed2k, fontName) = args;
        var fileInfo = new FileInfo(SubtitleService.GetFontPath(ed2k, fontName));
        if (!fileInfo.Exists)
        {
            await _subtitleService.ExtractFonts(ed2k);
            fileInfo.Refresh();
            if (!fileInfo.Exists)
                return TypedResults.NotFound();
        }

        if (!new FileExtensionContentTypeProvider().TryGetContentType(fontName, out var mimeType))
            mimeType = "font/otf";
        return TypedResults.PhysicalFile(fileInfo.FullName, mimeType);
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
