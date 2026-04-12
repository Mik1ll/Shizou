using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Shizou.Data;
using Shizou.Data.Database;
using Shizou.Data.Models;
using Swashbuckle.AspNetCore.Annotations;

namespace Shizou.Server.Controllers;

[ApiController]
[Route($"{Constants.ApiPrefix}/[controller]")]
public class AniDbNormalFiles(IShizouContext context) : ControllerBase
{
    [HttpGet]
    [SwaggerResponse(StatusCodes.Status200OK, type: typeof(List<AniDbNormalFile>))]
    public Ok<List<AniDbNormalFile>> GetAll() => EntityEndpoints.GetAll(context.AniDbNormalFiles);

    [HttpGet("{id:int}")]
    [SwaggerResponse(StatusCodes.Status200OK, type: typeof(AniDbNormalFile))]
    [SwaggerResponse(StatusCodes.Status404NotFound)]
    public Results<Ok<AniDbNormalFile>, NotFound> Get([FromRoute] int id)
        => EntityEndpoints.GetById(context.AniDbNormalFiles, e => e.Id == id);

    [HttpGet("[action]/{id:int}")]
    [SwaggerResponse(StatusCodes.Status200OK, type: typeof(List<AniDbNormalFile>))]
    [SwaggerResponse(StatusCodes.Status404NotFound)]
    public Results<Ok<List<AniDbNormalFile>>, NotFound> ByAniDbAnimeId([FromRoute] int id)
    {
        if (!context.AniDbAnimes.Any(a => a.Id == id))
            return TypedResults.NotFound();
        return TypedResults.Ok(context.AniDbNormalFiles.AsNoTracking().Where(f => f.AniDbEpisodes.Any(e => e.AniDbAnimeId == id)).ToList());
    }
}
