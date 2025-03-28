﻿using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Shizou.Data;
using Shizou.Server.Options;
using Swashbuckle.AspNetCore.Annotations;

namespace Shizou.Server.Controllers;

[ApiController]
[Route($"{Constants.ApiPrefix}/[controller]")]
public class Options : ControllerBase
{
    private readonly ShizouOptions _options;

    public Options(IOptionsSnapshot<ShizouOptions> options)
    {
        _options = options.Value;
    }

    /// <summary>
    ///     Gets current settings.
    /// </summary>
    /// <returns></returns>
    [HttpGet]
    [SwaggerResponse(StatusCodes.Status200OK, type: typeof(ShizouOptions))]
    public Ok<ShizouOptions> Get()
    {
        return TypedResults.Ok(_options);
    }

    /// <summary>
    ///     Overwrites all settings.
    /// </summary>
    /// <param name="options"></param>
    /// <returns></returns>
    [HttpPut]
    [SwaggerResponse(StatusCodes.Status200OK)]
    public Ok Save([FromBody] ShizouOptions options)
    {
        options.SaveToFile();
        return TypedResults.Ok();
    }
}
