using System.IO;
using System.Linq;
using System.Threading.Tasks;
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
    public async Task<Results<PhysicalFileHttpResult, NotFound>> GetEpisodeThumbnail(int episodeId)
    {
        var ed2K = await _imageService.GetEpisodeThumbnailAsync(episodeId).ConfigureAwait(false);
        if (ed2K is null)
            return TypedResults.NotFound();
        var fileInfo = new FileInfo(FilePaths.ExtraFileData.ThumbnailPath(ed2K));

        _contentTypeProvider.TryGetContentType(fileInfo.Name, out var mimeType);
        return TypedResults.PhysicalFile(fileInfo.FullName, mimeType);
    }
}
