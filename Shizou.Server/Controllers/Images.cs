using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.StaticFiles;
using Shizou.Data.Database;
using Shizou.Server.Services;
using Swashbuckle.AspNetCore.Annotations;

namespace Shizou.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class Images : ControllerBase
{
    private readonly ImageService _imageService;
    private readonly ShizouContext _context;
    private readonly IContentTypeProvider _contentTypeProvider;

    public Images(ImageService imageService, ShizouContext context, IContentTypeProvider contentTypeProvider)
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
        var path = ImageService.GetAnimePosterPath(posterName);
        _contentTypeProvider.TryGetContentType(posterName, out var mimeType);
        return TypedResults.PhysicalFile(path, mimeType);
    }

    [HttpGet("[action]/{episodeId:int}")]
    [SwaggerResponse(StatusCodes.Status200OK)]
    [SwaggerResponse(StatusCodes.Status404NotFound)]
    // ReSharper disable once InconsistentNaming
    public async Task<Results<PhysicalFileHttpResult, NotFound>> GetEpisodeThumbnail(int episodeId)
    {
        // ReSharper disable once InconsistentNaming
        var ed2k = await _imageService.GetEpisodeThumbnail(episodeId);
        if (ed2k is null)
            return TypedResults.NotFound();
        var fileInfo = new FileInfo(ImageService.GetFileThumbnailPath(ed2k));

        _contentTypeProvider.TryGetContentType(fileInfo.Name, out var mimeType);
        return TypedResults.PhysicalFile(fileInfo.FullName, mimeType);
    }
}
