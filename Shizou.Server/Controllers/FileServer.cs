using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Net.Mime;
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
    private const double SegmentDuration = 6d;
    private static readonly ConcurrentDictionary<string, SemaphoreSlim> FontLocks = new();
    private readonly IShizouContext _context;
    private readonly SubtitleService _subtitleService;
    private readonly LinkGenerator _linkGenerator;
    private readonly FfmpegService _ffmpegService;

    public FileServer(IShizouContext context, SubtitleService subtitleService, LinkGenerator linkGenerator, FfmpegService ffmpegService)
    {
        _context = context;
        _subtitleService = subtitleService;
        _linkGenerator = linkGenerator;
        _ffmpegService = ffmpegService;
    }

    /// <summary>
    ///     Get file by local Id, can optionally end in arbitrary extension
    /// </summary>
    /// <param name="ed2K"></param>
    /// <returns></returns>
    [HttpGet("ByEd2k/{ed2K}")]
    [SwaggerResponse(StatusCodes.Status404NotFound)]
    [SwaggerResponse(StatusCodes.Status416RangeNotSatisfiable)]
    [SwaggerResponse(StatusCodes.Status206PartialContent, null, typeof(Stream), MediaTypeNames.Application.Octet)]
    [SwaggerResponse(StatusCodes.Status200OK, null, typeof(Stream), MediaTypeNames.Application.Octet)]
    public Results<PhysicalFileHttpResult, NotFound> Get([FromRoute] string ed2K)
    {
        var localFile = _context.LocalFiles.AsNoTracking()
            .Where(e => e.ImportFolder != null && e.Ed2k == ed2K)
            .Select(lf => new { lf.ImportFolder!.Path, lf.PathTail })
            .FirstOrDefault();
        if (localFile is null)
            return TypedResults.NotFound();
        var filePath = Path.Combine(localFile.Path, localFile.PathTail);
        if (!System.IO.File.Exists(filePath))
            return TypedResults.NotFound();
        return TypedResults.PhysicalFile(filePath, MediaTypeNames.Application.Octet, Path.GetFileName(filePath), enableRangeProcessing: true);
    }

    [HttpGet("ByEd2k/{ed2K}/playlist.m3u8")]
    [SwaggerResponse(StatusCodes.Status404NotFound)]
    [SwaggerResponse(StatusCodes.Status200OK, null, typeof(Stream), "application/x-mpegURL")]
    public async Task<Results<FileContentHttpResult, NotFound>> GetPlaylist([FromRoute] string ed2K)
    {
        var m3U8 = new StringBuilder("#EXTM3U\n");
        m3U8.AppendLine("#EXT-X-VERSION:3");
        m3U8.AppendLine($"#EXT-X-TARGETDURATION:{SegmentDuration}"); // Max duration of a segment
        m3U8.AppendLine("#EXT-X-MEDIA-SEQUENCE:0");
        m3U8.AppendLine("#EXT-X-PLAYLIST-TYPE:VOD");

        var localFile = _context.LocalFiles.AsNoTracking()
            .Include(lf => lf.ImportFolder)
            .FirstOrDefault(lf => lf.ImportFolder != null && lf.Ed2k == ed2K);
        if (localFile is null)
            return TypedResults.NotFound();

        var duration = await _ffmpegService.GetDurationAsync(localFile).ConfigureAwait(false);
        if (duration is null)
            return TypedResults.NotFound();

        var totalSegments = (int)Math.Ceiling(duration.Value / SegmentDuration);

        for (var i = 0; i < totalSegments; i++)
        {
            // Last segment might be shorter
            var currentSegmentDuration = i == totalSegments - 1
                ? duration - i * SegmentDuration
                : SegmentDuration;

            m3U8.AppendLine($"#EXTINF:{currentSegmentDuration:F6},");
            // The URL for the specific segment
            m3U8.AppendLine(GetFileUri(ed2K, i));
        }

        m3U8.AppendLine("#EXT-X-ENDLIST");

        return TypedResults.File(Encoding.UTF8.GetBytes(m3U8.ToString()), "application/x-mpegURL", "playlist.m3u8");

        string GetFileUri(string fileEd2K, int segment, AniDbEpisode? ep = null)
        {
            IDictionary<string, object?> values = new ExpandoObject();
            values["ed2K"] = fileEd2K;
            values["segment"] = segment;
            if (ep is not null)
            {
                values["posterFilename"] = ep.AniDbAnime.ImageFilename;
                values["animeName"] = ep.AniDbAnime.TitleEnglish ?? ep.AniDbAnime.TitleTranscription;
                values["episodeName"] = ep.TitleEnglish;
                values["epNo"] = ep.EpString;
                values["epCount"] = ep.AniDbAnime.EpisodeCount;
                values["animeId"] = ep.AniDbAnimeId;
                values["restricted"] = ep.AniDbAnime.Restricted;
                values["appId"] = "07a58b50-5109-5aa3-abbc-782fed0df04f";
            }

            values[IdentityConstants.ApplicationScheme] = HttpContext.Request.Cookies[IdentityConstants.ApplicationScheme];
            var fileUri = _linkGenerator.GetUriByAction(HttpContext ?? throw new InvalidOperationException(), "GetSegment",
                "FileServer", values) ?? throw new ArgumentException("Could not generate file uri");
            return fileUri;
        }
    }

    /// <summary>
    ///     Get file by local Id, can optionally end in arbitrary extension
    /// </summary>
    /// <param name="ed2K"></param>
    /// <param name="segment"></param>
    /// <returns></returns>
    [HttpGet("ByEd2k/{ed2K}/{segment:int}.ts")]
    [SwaggerResponse(StatusCodes.Status404NotFound)]
    [SwaggerResponse(StatusCodes.Status416RangeNotSatisfiable)]
    [SwaggerResponse(StatusCodes.Status206PartialContent, null, typeof(Stream), "video/mp2t")]
    [SwaggerResponse(StatusCodes.Status200OK, null, typeof(Stream), "video/mp2t")]
    public async Task<Results<PhysicalFileHttpResult, NotFound>> GetSegmentAsync([FromRoute] string ed2K, [FromRoute] int segment)
    {
        var segmentPath = FilePaths.VideoSegmentPath(ed2K, segment);
        if (Path.Exists(segmentPath))
            return TypedResults.PhysicalFile(segmentPath, "video/mp2t", Path.GetFileName(segmentPath), enableRangeProcessing: true);

        var localFile = _context.LocalFiles.AsNoTracking()
            .Include(lf => lf.ImportFolder)
            .FirstOrDefault(e => e.ImportFolder != null && e.Ed2k == ed2K);
        if (localFile is null)
            return TypedResults.NotFound();

        var startTime = segment * SegmentDuration;

        await _ffmpegService.GenerateSegmentAsync(localFile, segment, startTime, SegmentDuration).ConfigureAwait(false);

        return TypedResults.PhysicalFile(segmentPath, MediaTypeNames.Application.Octet, Path.GetFileName(segmentPath), enableRangeProcessing: true);
    }

    /// <summary>
    ///     Get embedded ASS subtitle of local file
    /// </summary>
    /// <param name="ed2K"></param>
    /// <param name="index"></param>
    /// <returns></returns>
    [HttpGet("ByEd2k/{ed2K}/Subtitles/{index:int}")]
    [SwaggerResponse(StatusCodes.Status404NotFound)]
    [SwaggerResponse(StatusCodes.Status200OK, null, typeof(Stream), "text/x-ssa")]
    public Results<PhysicalFileHttpResult, NotFound> GetSubtitle([FromRoute] string ed2K, [FromRoute] int index)
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
    [HttpGet("ByEd2k/{ed2K}/Fonts/{fontName}")]
    [SwaggerResponse(StatusCodes.Status404NotFound)]
    [SwaggerResponse(StatusCodes.Status200OK, null, typeof(Stream), "font/ttf", "font/otf")]
    public async Task<Results<PhysicalFileHttpResult, NotFound>> GetFont([FromRoute] string ed2K, [FromRoute] string fontName)
    {
        if (!SubtitleService.ValidFontFormats.Any(f => fontName.EndsWith(f, StringComparison.OrdinalIgnoreCase)))
            return TypedResults.NotFound();
        var fontPath = await SubtitleService.GetAttachmentPathAsync(ed2K, fontName).ConfigureAwait(false);
        var fontLock = FontLocks.GetOrAdd(ed2K, new SemaphoreSlim(1));
        await fontLock.WaitAsync().ConfigureAwait(false);
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

    [HttpGet("ByAnimeId/{animeId:int}/upnext.m3u8")]
    [SwaggerResponse(StatusCodes.Status404NotFound)]
    [SwaggerResponse(StatusCodes.Status200OK, null, typeof(Stream), "application/x-mpegurl")]
    public Results<FileContentHttpResult, NotFound> GetUpNext([FromRoute] int animeId)
    {
        var m3U8 = "#EXTM3U\n";

        var eps = _context.AniDbEpisodes.AsNoTracking()
            .Include(e => e.AniDbAnime)
            .Include(e => e.AniDbFiles)
            .ThenInclude(f => f.LocalFiles)
            .Where(e => e.AniDbAnimeId == animeId)
            .OrderBy(e => e.EpisodeType)
            .ThenBy(e => e.Number).ToList();
        var episode = eps.Where(e => e.AniDbFiles.Any(f => f.LocalFiles.Count != 0) && e.AniDbFiles.All(f => !f.FileWatchedState.Watched))
            .OrderBy(e => e.EpisodeType).ThenBy(e => e.Number).FirstOrDefault();
        var localFile = episode?.AniDbFiles.SelectMany(f => f.LocalFiles).FirstOrDefault();
        if (episode is null || localFile is null)
            return TypedResults.NotFound();
        var groupId = eps.SelectMany(ep => ep.AniDbFiles)
            .OfType<AniDbNormalFile>()
            .FirstOrDefault(f => f.LocalFiles.Any(lf => localFile.Ed2k == lf.Ed2k))?.AniDbGroupId;
        var lastEpNo = episode.Number - 1;
        foreach (var loopEp in eps)
        {
            if (loopEp.EpisodeType != episode.EpisodeType || loopEp.Number != lastEpNo + 1)
                break;
            var loopLocalFile = loopEp.AniDbFiles.OrderBy(l => (l as AniDbNormalFile)?.AniDbGroupId != groupId).SelectMany(f => f.LocalFiles).FirstOrDefault();
            if (loopLocalFile is null)
                break;
            m3U8 += $"#EXTINF:-1,{episode.AniDbAnime.TitleTranscription} - {loopEp.EpString}\n";
            m3U8 += $"{GetFileUri(loopLocalFile.Ed2k, loopEp)}\n";
            lastEpNo = loopEp.Number;
        }

        return TypedResults.File(Encoding.UTF8.GetBytes(m3U8), "application/x-mpegurl", "playlist.m3u8");

        string GetFileUri(string fileEd2K, AniDbEpisode? ep = null)
        {
            IDictionary<string, object?> values = new ExpandoObject();
            values["ed2K"] = fileEd2K;
            if (ep is not null)
            {
                values["posterFilename"] = ep.AniDbAnime.ImageFilename;
                values["animeName"] = ep.AniDbAnime.TitleEnglish ?? ep.AniDbAnime.TitleTranscription;
                values["episodeName"] = ep.TitleEnglish;
                values["epNo"] = ep.EpString;
                values["epCount"] = ep.AniDbAnime.EpisodeCount;
                values["animeId"] = ep.AniDbAnimeId;
                values["restricted"] = ep.AniDbAnime.Restricted;
                values["appId"] = "07a58b50-5109-5aa3-abbc-782fed0df04f";
            }

            values[IdentityConstants.ApplicationScheme] = HttpContext.Request.Cookies[IdentityConstants.ApplicationScheme];
            var fileUri = _linkGenerator.GetUriByAction(HttpContext ?? throw new InvalidOperationException(), nameof(Get),
                nameof(FileServer), values) ?? throw new ArgumentException("Could not generate file uri");
            return fileUri;
        }
    }
}
