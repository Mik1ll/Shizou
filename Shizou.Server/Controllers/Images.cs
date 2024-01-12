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

    [HttpPut("[action]")]
    [SwaggerResponse(StatusCodes.Status200OK)]
    public Ok GetMissingAnimePosters()
    {
        _imageService.GetMissingAnimePosters();
        return TypedResults.Ok();
    }

    [HttpGet("[action]/{animeId:int}")]
    [SwaggerResponse(StatusCodes.Status200OK)]
    [SwaggerResponse(StatusCodes.Status404NotFound)]
    public Results<PhysicalFileHttpResult, NotFound> GetAnimePoster(int animeId)
    {
        var posterName = _context.AniDbAnimes.Where(a => a.Id == animeId).Select(a => a.ImageFilename).FirstOrDefault();
        if (posterName is null)
            return TypedResults.NotFound();
        var path = FilePaths.AnimePosterPath(posterName);
        _contentTypeProvider.TryGetContentType(posterName, out var mimeType);
        return TypedResults.PhysicalFile(path, mimeType);
    }

    [HttpGet("[action]/{episodeId:int}")]
    [SwaggerResponse(StatusCodes.Status200OK)]
    [SwaggerResponse(StatusCodes.Status404NotFound)]
    public Results<PhysicalFileHttpResult, NotFound> GetEpisodeThumbnail(int episodeId)
    {
        if (_imageService.GetEpisodeThumbnail(episodeId) is not { Exists: true } thumbnail)
            return TypedResults.NotFound();
        _contentTypeProvider.TryGetContentType(thumbnail.Name, out var mimeType);
        return TypedResults.PhysicalFile(thumbnail.FullName, mimeType, $"thumb_{thumbnail.Name}");
    }
}
