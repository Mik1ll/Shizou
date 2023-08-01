using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Shizou.Server.Services;
using Swashbuckle.AspNetCore.Annotations;

namespace Shizou.Server.Controllers;

[ApiController]
[Route("[controller]")]
public class ImagesController : ControllerBase
{
    private readonly ImageService _imageService;

    public ImagesController(ImageService imageService)
    {
        _imageService = imageService;
    }

    [HttpPut("[action]")]
    [SwaggerResponse(StatusCodes.Status200OK)]
    public ActionResult GetMissingAnimePosters()
    {
        _imageService.GetMissingAnimePosters();
        return Ok();
    }
}
