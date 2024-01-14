using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Shizou.Data;
using Shizou.Data.Database;
using Shizou.Data.Enums;
using Shizou.Server.Extensions.Query;
using Shizou.Server.Services;
using Swashbuckle.AspNetCore.Annotations;

namespace Shizou.Server.Controllers;

[ApiController]
[Route($"{Constants.ApiPrefix}/[controller]")]
public class FileServer : ControllerBase
{
    private static readonly ConcurrentDictionary<string, SemaphoreSlim> FontLocks = new();
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
    /// <param name="ed2K"></param>
    /// <param name="identityCookie"></param>
    /// <returns></returns>
    [HttpGet("{ed2K}")]
    [SwaggerResponse(StatusCodes.Status404NotFound)]
    [SwaggerResponse(StatusCodes.Status416RangeNotSatisfiable)]
    [SwaggerResponse(StatusCodes.Status206PartialContent, contentTypes: "application/octet-stream")]
    [SwaggerResponse(StatusCodes.Status200OK, contentTypes: "application/octet-stream")]
    // ReSharper disable once ParameterOnlyUsedForPreconditionCheck.Global
    public Results<FileStreamHttpResult, NotFound> Get(string ed2K, [FromQuery] string? identityCookie = null)
    {
        if (!string.Equals(nameof(identityCookie), Constants.IdentityCookieName, StringComparison.OrdinalIgnoreCase))
            throw new ApplicationException("Identity cookie must match name of constant");
        var localFile = _context.LocalFiles.Include(e => e.ImportFolder).FirstOrDefault(e => e.Ed2k == ed2K);
        if (localFile is null)
            return TypedResults.NotFound();
        if (localFile.ImportFolder is null)
        {
            _logger.LogWarning("Tried to get local file with no import folder");
            return TypedResults.NotFound();
        }

        var fileInfo = new FileInfo(Path.GetFullPath(Path.Combine(localFile.ImportFolder.Path, localFile.PathTail)));
        if (!fileInfo.Exists)
            return TypedResults.NotFound();
        if (!_contentTypeProvider.TryGetContentType(fileInfo.Name, out var mimeType))
            mimeType = "application/octet-stream";
        var fileStream = new FileStream(fileInfo.FullName, FileMode.Open, FileAccess.Read, FileShare.Read, 1 << 19, FileOptions.Asynchronous);
        return TypedResults.File(fileStream, mimeType, fileInfo.Name, enableRangeProcessing: true);
    }

    [HttpGet("{ed2K}/Playlist")]
    [SwaggerResponse(StatusCodes.Status404NotFound)]
    [SwaggerResponse(StatusCodes.Status200OK)]
    public Results<FileContentHttpResult, NotFound> GetPlaylist(string ed2K, [FromQuery] bool? single, [FromQuery] string? identityCookie)
    {
        if (!string.Equals(nameof(identityCookie), Constants.IdentityCookieName, StringComparison.OrdinalIgnoreCase))
            throw new ApplicationException("Identity cookie must match name of constant");
        var m3U8 = "#EXTM3U\n";

        var anime = _context.AniDbEpisodes.AsNoTracking()
            .Where(ep => ep.ManualLinkLocalFiles.Any(lf => lf.Ed2k == ed2K) || ep.AniDbFiles.Any(f => f.LocalFile!.Ed2k == ed2K))
            .Select(ep => ep.AniDbAnime)
            .FirstOrDefault();
        if (anime is null)
            return TypedResults.NotFound();
        var eps = (from e in _context.AniDbEpisodes.HasLocalFiles()
            where e.AniDbAnimeId == anime.Id
            orderby e.EpisodeType, e.Number
            select new
            {
                EpType = e.EpisodeType,
                EpNo = e.Number,
                ManLocals = e.ManualLinkLocalFiles.Select(lf => new { lf.Ed2k, AniDbGroupId = (int?)null }),
                Locals = e.AniDbFiles.Select(f => new { f.LocalFile!.Ed2k, f.AniDbGroupId })
            }).ToList();
        var ep = eps.First(ep => ep.Locals.Any(l => l.Ed2k == ed2K) || ep.ManLocals.Any(ml => ml.Ed2k == ed2K));
        if (single is true)
            eps = new[] { ep }.ToList();
        var localFile = ep.Locals.Concat(ep.ManLocals).First(l => l.Ed2k == ed2K);
        var lastEpNo = ep.EpNo - 1;
        var lastEpType = ep.EpType;
        foreach (var loopEp in eps.SkipWhile(x => x != ep))
        {
            if (lastEpType != loopEp.EpType || lastEpNo != loopEp.EpNo - 1)
                break;
            var loopLocalFile = loopEp.Locals.Concat(loopEp.ManLocals).FirstOrDefault(l => l.AniDbGroupId == localFile.AniDbGroupId);
            if (loopLocalFile is null)
                break;
            m3U8 += $"#EXTINF:-1,{anime.TitleTranscription} - {loopEp.EpType.GetEpString(loopEp.EpNo)}\n";
            var fileUri = GetFileUri(loopLocalFile.Ed2k, identityCookie);
            m3U8 += $"{fileUri}\n";
            lastEpType = loopEp.EpType;
            lastEpNo = loopEp.EpNo;
        }


        _contentTypeProvider.TryGetContentType(".m3u", out var mimeType);
        return TypedResults.File(Encoding.UTF8.GetBytes(m3U8), mimeType, $"{ed2K}.m3u8");
    }

    /// <summary>
    ///     Get embedded ASS subtitle of local file
    /// </summary>
    /// <param name="ed2K"></param>
    /// <param name="index"></param>
    /// <returns></returns>
    [HttpGet("{ed2K}/Subtitles/{index:int}")]
    [SwaggerResponse(StatusCodes.Status404NotFound)]
    [SwaggerResponse(StatusCodes.Status200OK, contentTypes: "text/x-ssa")]
    public Results<PhysicalFileHttpResult, NotFound> GetSubtitle(string ed2K, int index)
    {
        var fileInfo = new FileInfo(FilePaths.ExtraFileData.SubPath(ed2K, index));
        if (!fileInfo.Exists)
            return TypedResults.NotFound();

        return TypedResults.PhysicalFile(fileInfo.FullName, "text/x-ssa", fileInfo.Name);
    }


    /// <summary>
    ///     Get embedded font of local file
    /// </summary>
    /// <param name="ed2K"></param>
    /// <param name="fontName"></param>
    /// <returns></returns>
    [HttpGet("{ed2k}/Fonts/{fontName}")]
    [SwaggerResponse(StatusCodes.Status404NotFound)]
    [SwaggerResponse(StatusCodes.Status200OK, contentTypes: new[] { "font/ttf", "font/otf" })]
    public async Task<Results<PhysicalFileHttpResult, NotFound>> GetFont(string ed2K, string fontName)
    {
        if (!SubtitleService.ValidFontFormats.Any(f => fontName.EndsWith(f, StringComparison.OrdinalIgnoreCase)))
            return TypedResults.NotFound();
        var fontLock = FontLocks.GetOrAdd(ed2K, new SemaphoreSlim(1));
        await fontLock.WaitAsync().ConfigureAwait(false);
        var fontPath = await SubtitleService.GetAttachmentPathAsync(ed2K, fontName).ConfigureAwait(false);
        try
        {
            if (!System.IO.File.Exists(fontPath))
            {
                await _subtitleService.ExtractAttachmentsAsync(ed2K).ConfigureAwait(false);
                fontPath = await SubtitleService.GetAttachmentPathAsync(ed2K, fontName).ConfigureAwait(false);
                if (!System.IO.File.Exists(fontPath))
                    return TypedResults.NotFound();
            }
        }
        finally
        {
            fontLock.Release();
        }

        if (!new FileExtensionContentTypeProvider().TryGetContentType(fontName, out var mimeType))
            mimeType = "font/otf";
        return TypedResults.PhysicalFile(fontPath, mimeType, Path.GetFileName(fontPath));
    }

    private string GetFileUri(string ed2K, string? identityCookie)
    {
        IDictionary<string, object?> values = new ExpandoObject();
        values["ed2K"] = ed2K;
        values[Constants.IdentityCookieName] = identityCookie;
        var fileUri = _linkGenerator.GetUriByAction(HttpContext ?? throw new InvalidOperationException(), nameof(Get),
            nameof(FileServer), values) ?? throw new ArgumentException();
        return fileUri;
    }
}
