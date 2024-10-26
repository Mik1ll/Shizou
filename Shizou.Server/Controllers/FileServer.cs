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
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Shizou.Data;
using Shizou.Data.Database;
using Shizou.Data.Models;
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
    /// <returns></returns>
    [HttpGet("{ed2K}")]
    [SwaggerResponse(StatusCodes.Status404NotFound)]
    [SwaggerResponse(StatusCodes.Status416RangeNotSatisfiable)]
    [SwaggerResponse(StatusCodes.Status206PartialContent, contentTypes: "application/octet-stream")]
    [SwaggerResponse(StatusCodes.Status200OK, contentTypes: "application/octet-stream")]
    public Results<FileStreamHttpResult, NotFound> Get(string ed2K)
    {
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
    [SwaggerResponse(StatusCodes.Status200OK, contentTypes: "application/x-mpegURL")]
    public Results<FileContentHttpResult, NotFound> GetPlaylist(string ed2K, [FromQuery] bool? single)
    {
        var m3U8 = "#EXTM3U\n";

        var animeId = _context.AniDbEpisodes.AsNoTracking()
            .Where(ep => ep.AniDbFiles.Any(f => f.LocalFiles.Any(lf => lf.Ed2k == ed2K)))
            .Select(ep => (int?)ep.AniDbAnimeId).FirstOrDefault();
        if (animeId is null)
            return TypedResults.NotFound();
        List<AniDbEpisode> eps = (from e in _context.AniDbEpisodes
                .Include(e => e.AniDbAnime)
                .Include(e => e.AniDbFiles)
                .ThenInclude(f => f.LocalFiles).AsNoTracking()
            where e.AniDbAnimeId == animeId
            orderby e.EpisodeType, e.Number
            select e).ToList();
        var localFile = eps.SelectMany(ep => ep.AniDbFiles.SelectMany(f => f.LocalFiles)).First(l => l.Ed2k == ed2K);
        var episode = eps.First(ep => ep.AniDbFiles.Any(f => f.LocalFiles.Any(lf => lf.Ed2k == ed2K)));
        var groupId = (localFile.AniDbFile as AniDbNormalFile)?.AniDbGroupId;
        var epCount = episode.AniDbAnime.EpisodeCount;
        var lastEpNo = episode.Number - 1;
        foreach (var loopEp in eps.SkipWhile(x => x != episode))
        {
            if (loopEp.EpisodeType != episode.EpisodeType || loopEp.Number != lastEpNo + 1)
                break;
            var loopLocalFile = loopEp.AniDbFiles.OrderBy(l => (l as AniDbNormalFile)?.AniDbGroupId != groupId).SelectMany(f => f.LocalFiles).FirstOrDefault();
            if (loopLocalFile is null)
                break;
            m3U8 += $"#EXTINF:-1,{episode.AniDbAnime.TitleTranscription} - {loopEp.EpString}\n";
            var fileUri = GetFileUri(loopLocalFile, loopEp);
            m3U8 += $"{fileUri}\n";
            lastEpNo = loopEp.Number;
            if (single is true)
                break;
        }

        _contentTypeProvider.TryGetContentType(".m3u", out var mimeType);
        return TypedResults.File(Encoding.UTF8.GetBytes(m3U8), mimeType, $"{ed2K}.m3u8");

        string GetFileUri(LocalFile lf, AniDbEpisode ep)
        {
            IDictionary<string, object?> values = new ExpandoObject();
            values["ed2K"] = lf.Ed2k;
            values["posterFilename"] = ep.AniDbAnime.ImageFilename;
            values["animeName"] = ep.AniDbAnime.TitleEngish ?? ep.AniDbAnime.TitleTranscription;
            values["episodeName"] = ep.TitleEnglish;
            values["epNo"] = ep.EpString;
            values["epCount"] = epCount;
            values["animeId"] = animeId;
            values["restricted"] = ep.AniDbAnime.Restricted;
            values[IdentityConstants.ApplicationScheme] = HttpContext.Request.Cookies[IdentityConstants.ApplicationScheme];
            values["appId"] = "07a58b50-5109-5aa3-abbc-782fed0df04f";
            var fileUri = _linkGenerator.GetUriByAction(HttpContext ?? throw new InvalidOperationException(), nameof(Get),
                nameof(FileServer), values) ?? throw new ArgumentException();
            return fileUri;
        }
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
    [SwaggerResponse(StatusCodes.Status200OK, contentTypes: ["font/ttf", "font/otf"])]
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
}
