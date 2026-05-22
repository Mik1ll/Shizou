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
public class AniDbEpisodes(IShizouContext context) : ControllerBase
{
    [HttpGet]
    [SwaggerResponse(StatusCodes.Status200OK, type: typeof(List<AniDbEpisode>))]
    public Ok<List<AniDbEpisode>> GetAll() => EntityEndpoints.GetAll(context.AniDbEpisodes);

    [HttpGet("{id:int}")]
    [SwaggerResponse(StatusCodes.Status200OK, type: typeof(AniDbEpisode))]
    [SwaggerResponse(StatusCodes.Status404NotFound)]
    public Results<Ok<AniDbEpisode>, NotFound> Get([FromRoute] int id)
        => EntityEndpoints.GetById(context.AniDbEpisodes, e => e.Id == id);

    [HttpGet("[action]/{id:int}")]
    [SwaggerResponse(StatusCodes.Status200OK, type: typeof(List<AniDbEpisode>))]
    [SwaggerResponse(StatusCodes.Status404NotFound)]
    public Results<Ok<List<AniDbEpisode>>, NotFound> ByAniDbFileId([FromRoute] int id)
    {
        if (!context.AniDbFiles.Any(f => f.Id == id))
            return TypedResults.NotFound();
        var result = context.AniDbEpisodes.AsNoTracking().Where(e => e.AniDbEpisodeFileXrefs.Any(xref => xref.AniDbFileId == id)).ToList();
        return TypedResults.Ok(result);
    }

    [HttpGet("[action]/{id:int}")]
    [SwaggerResponse(StatusCodes.Status200OK, type: typeof(List<AniDbEpisode>))]
    [SwaggerResponse(StatusCodes.Status404NotFound)]
    public Results<Ok<List<AniDbEpisode>>, NotFound> ByAniDbAnimeId([FromRoute] int id)
    {
        if (!context.AniDbAnimes.Any(a => a.Id == id))
            return TypedResults.NotFound();
        return TypedResults.Ok(context.AniDbEpisodes.AsNoTracking().Where(e => e.AniDbAnimeId == id).ToList());
    }
}
