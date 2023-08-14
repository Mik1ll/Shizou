using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Shizou.Server.Options;
using Swashbuckle.AspNetCore.Annotations;

namespace Shizou.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class OptionsController : ControllerBase
{
    private readonly ShizouOptions _options;

    public OptionsController(IOptionsSnapshot<ShizouOptions> options)
    {
        _options = options.Value;
    }

    /// <summary>
    ///     Gets current settings.
    /// </summary>
    /// <returns></returns>
    [HttpGet]
    [SwaggerResponse(StatusCodes.Status200OK)]
    [Produces("application/json")]
    public ActionResult<ShizouOptions> Get()
    {
        return Ok(_options);
    }

    /// <summary>
    ///     Overwrites all settings.
    /// </summary>
    /// <param name="options"></param>
    /// <returns></returns>
    [HttpPut]
    public ActionResult Save([FromBody] ShizouOptions options)
    {
        options.SaveToFile();
        return Ok();
    }
}