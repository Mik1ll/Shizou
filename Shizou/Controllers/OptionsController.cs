using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Shizou.Options;

namespace Shizou.Controllers;

[ApiController]
[Route("[controller]")]
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
    [ProducesResponseType(StatusCodes.Status200OK)]
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