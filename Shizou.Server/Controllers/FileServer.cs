using System;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Shizou.Data;
using Shizou.Data.Database;
using Shizou.Server.Extensions.Query;
using Shizou.Server.Services;
using Swashbuckle.AspNetCore.Annotations;

namespace Shizou.Server.Controllers;

[ApiController]
[Route($"{Constants.ApiPrefix}/[controller]")]
public class FileServer : ControllerBase
{
    private readonly IShizouContext _context;
    private readonly SubtitleService _subtitleService;
    private readonly ILogger<FileServer> _logger;
    private readonly LinkGenerator _linkGenerator;
    private readonly IContentTypeProvider _contentTypeProvider;

    public FileServer(ILogger<FileServer> logger, IShizouContext context, SubtitleService subtitleService, LinkGenerator linkGenerator,
        IContentTypeProvider contentTypeProvider)
    {
        _logger = logger;
        _context = context;
        _subtitleService = subtitleService;
        _linkGenerator = linkGenerator;
        _contentTypeProvider = contentTypeProvider;
    }

    /// <summary>
    ///     Get file by local Id, can optionally end in arbitrary extension
    /// </summary>
    /// <param name="localFileId"></param>
    /// <param name="identityCookie"></param>
    /// <returns></returns>
    [HttpGet("{localFileId}")]
    [SwaggerResponse(StatusCodes.Status404NotFound)]
    [SwaggerResponse(StatusCodes.Status416RangeNotSatisfiable)]
    [SwaggerResponse(StatusCodes.Status409Conflict)]
    [SwaggerResponse(StatusCodes.Status206PartialContent, contentTypes: "application/octet-stream")]
    [SwaggerResponse(StatusCodes.Status200OK, contentTypes: "application/octet-stream")]
    public Results<FileStreamHttpResult, Conflict<string>, NotFound> Get(string localFileId, [FromQuery] string? identityCookie = null)
    {
        if (!string.Equals(nameof(identityCookie), Constants.IdentityCookieName, StringComparison.OrdinalIgnoreCase))
            throw new ApplicationException("Identity cookie must match name of constant");
        var split = localFileId.Split('.', 2);
        var id = int.Parse(split[0]);
        var localFile = _context.LocalFiles.Include(e => e.ImportFolder).FirstOrDefault(e => e.Id == id);
        if (localFile is null || (split.Length == 2 && split[1] != Path.GetExtension(localFile.PathTail)[1..]))
            return TypedResults.NotFound();
        if (localFile.ImportFolder is null)
        {
            _logger.LogWarning("Tried to get local file with no import folder");
            return TypedResults.Conflict("Import folder does not exist");
        }

        var fileInfo = new FileInfo(Path.GetFullPath(Path.Combine(localFile.ImportFolder.Path, localFile.PathTail)));
        if (!fileInfo.Exists)
            return TypedResults.Conflict("Local file path does not exist");
        if (!_contentTypeProvider.TryGetContentType(fileInfo.Name, out var mimeType))
            mimeType = "application/octet-stream";
        var fileStream = new FileStream(fileInfo.FullName, FileMode.Open, FileAccess.Read, FileShare.Read, 1 << 19, FileOptions.Asynchronous);
        return TypedResults.File(fileStream, mimeType, fileInfo.Name, enableRangeProcessing: true);
    }

    [HttpGet("[action]/{localFileId}")]
    [SwaggerResponse(StatusCodes.Status404NotFound)]
    [SwaggerResponse(StatusCodes.Status200OK)]
    public Results<FileContentHttpResult, NotFound> GetWithPlaylist(string localFileId, [FromQuery] bool? single, [FromQuery] string? identityCookie)
    {
        if (!string.Equals(nameof(identityCookie), Constants.IdentityCookieName, StringComparison.OrdinalIgnoreCase))
            throw new ApplicationException("Identity cookie must match name of constant");
        var split = localFileId.Split('.', 2);
        var id = int.Parse(split[0]);
        if (split.Length == 2 && split[1] != "m3u8" && split[1] != "m3u")
            return TypedResults.NotFound();
        var m3U8 = "#EXTM3U\n";
        if (single is true)
        {
            var lf = _context.LocalFiles.Select(lf => new { lf.Id, lf.PathTail }).FirstOrDefault(lf => lf.Id == id);
            if (lf is null)
                return TypedResults.NotFound();
            m3U8 += $"#EXTINF:-1,{Path.GetFileName(lf.PathTail)}\n";
            var fileUri = GetFileUri(identityCookie, lf.Id, lf.PathTail);
            m3U8 += $"{fileUri}\n";
        }
        else
        {
            var aid = _context.AniDbEpisodes
                .Where(ep => ep.ManualLinkLocalFiles.Any(lf => lf.Id == id) || ep.AniDbFiles.Any(f => f.LocalFile!.Id == id))
                .Select(ep => (int?)ep.AniDbAnimeId)
                .FirstOrDefault();
            if (aid is null)
                return TypedResults.NotFound();
            var eps = (from e in _context.AniDbEpisodes.HasLocalFiles()
                where e.AniDbAnimeId == aid
                orderby e.EpisodeType, e.Number
                select new
                {
                    EpType = e.EpisodeType,
                    EpNo = e.Number,
                    ManLocals = e.ManualLinkLocalFiles.Select(lf => new { lf.Id, lf.PathTail, AniDbGroupId = (int?)null }),
                    Locals = e.AniDbFiles.Select(f => new { f.LocalFile.Id, f.LocalFile.PathTail, f.AniDbGroupId })
                }).ToList();
            var ep = eps.First(ep => ep.Locals.Any(l => l.Id == id) || ep.ManLocals.Any(ml => ml.Id == id));
            var localFile = ep.Locals.Concat(ep.ManLocals).First(l => l.Id == id);
            var lastEpNo = ep.EpNo - 1;
            var lastEpType = ep.EpType;
            foreach (var loopEp in eps.SkipWhile(x => x != ep))
            {
                if (lastEpType != loopEp.EpType || lastEpNo != loopEp.EpNo - 1)
                    break;
                var lf = loopEp.Locals.Concat(loopEp.ManLocals).FirstOrDefault(l => l.AniDbGroupId == localFile.AniDbGroupId);
                if (lf is null)
                    break;
                m3U8 += $"#EXTINF:-1,{Path.GetFileName(lf.PathTail)}\n";
                var fileUri = GetFileUri(identityCookie, lf.Id, lf.PathTail);
                m3U8 += $"{fileUri}\n";
                lastEpType = loopEp.EpType;
                lastEpNo = loopEp.EpNo;
            }
        }

        _contentTypeProvider.TryGetContentType($"{id}.m3u", out var mimeType);
        return TypedResults.File(Encoding.UTF8.GetBytes(m3U8), mimeType, $"{id}.m3u8");
    }

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
        var (ed2K, index) = args;
        var fileInfo = new FileInfo(FilePaths.ExtraFileData.SubPath(ed2K, index));
        if (!fileInfo.Exists)
        {
            await _subtitleService.ExtractSubtitlesAsync(ed2K).ConfigureAwait(false);
            fileInfo.Refresh();
            if (!fileInfo.Exists)
                return TypedResults.NotFound();
        }

        return TypedResults.PhysicalFile(fileInfo.FullName, "text/x-ssa");
    }

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
        var (ed2K, fontName) = args;
        var fileInfo = new FileInfo(FilePaths.ExtraFileData.FontPath(ed2K, fontName));
        if (!fileInfo.Exists)
        {
            await _subtitleService.ExtractFontsAsync(ed2K).ConfigureAwait(false);
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

    private string GetFileUri(string? identityCookie, int localId, string pathTail)
    {
        IDictionary<string, object?> values = new ExpandoObject();
        values["LocalFileId"] = $"{localId}{Path.GetExtension(pathTail)}";
        values[Constants.IdentityCookieName] = identityCookie;
        var fileUri = _linkGenerator.GetUriByAction(HttpContext ?? throw new InvalidOperationException(), nameof(Get),
            nameof(FileServer), values) ?? throw new ArgumentException();
        return fileUri;
    }


    public record GetSubtitleArgs(string Ed2K, int Index);

    public record GetFontArgs(string Ed2K, string FontName);
}
