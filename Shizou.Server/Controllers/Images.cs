using System.IO;
using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.StaticFiles;
using Shizou.Data;
using Shizou.Data.Database;
using Shizou.Server.Services;
using Swashbuckle.AspNetCore.Annotations;

namespace Shizou.Server.Controllers;

[ApiController]
[Route($"{Constants.ApiPrefix}/[controller]")]
public class Images : ControllerBase
{
    private readonly ImageService _imageService;
    private readonly IShizouContext _context;
    private readonly IContentTypeProvider _contentTypeProvider;

    public Images(ImageService imageService, IShizouContext context, IContentTypeProvider contentTypeProvider)
    {
        _imageService = imageService;
        _context = context;
        _contentTypeProvider = contentTypeProvider;
    }

    [HttpPut("MissingAnimePosters")]
    [SwaggerResponse(StatusCodes.Status200OK)]
    public Ok GetMissingAnimePosters()
    {
        _imageService.GetMissingAnimePosters();
        return TypedResults.Ok();
    }

    [HttpGet("AnimePosters/{animeId:int}")]
    [SwaggerResponse(StatusCodes.Status200OK)]
    [SwaggerResponse(StatusCodes.Status404NotFound)]
    public Results<PhysicalFileHttpResult, NotFound> GetAnimePoster(int animeId)
    {
        var posterName = _context.AniDbAnimes.Where(a => a.Id == animeId).Select(a => a.ImageFilename).FirstOrDefault();
        if (posterName is null || new FileInfo(FilePaths.AnimePosterPath(posterName)) is not { Exists: true } poster)
            return TypedResults.NotFound();
        _contentTypeProvider.TryGetContentType(posterName, out var mimeType);
        return TypedResults.PhysicalFile(poster.FullName, mimeType, poster.Name, poster.LastWriteTimeUtc);
    }

    [HttpGet("EpisodeThumbnails/{episodeId:int}")]
    [SwaggerResponse(StatusCodes.Status200OK)]
    [SwaggerResponse(StatusCodes.Status404NotFound)]
    public Results<PhysicalFileHttpResult, NotFound> GetEpisodeThumbnail(int episodeId)
    {
        if (_imageService.GetEpisodeThumbnail(episodeId) is not { Exists: true } thumbnail)
            return TypedResults.NotFound();
        _contentTypeProvider.TryGetContentType(thumbnail.Name, out var mimeType);
        return TypedResults.PhysicalFile(thumbnail.FullName, mimeType, thumbnail.Name, thumbnail.LastWriteTimeUtc);
    }
}
