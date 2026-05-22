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
public class AniDbEpisodeFileXrefs(IShizouContext context) : ControllerBase
{
    [HttpGet]
    [SwaggerResponse(StatusCodes.Status200OK, type: typeof(List<AniDbEpisodeFileXref>))]
    public Ok<List<AniDbEpisodeFileXref>> GetAll() => TypedResults.Ok(context.AniDbEpisodeFileXrefs.AsNoTracking().ToList());

    [HttpGet("[action]/{id:int}")]
    [SwaggerResponse(StatusCodes.Status200OK, type: typeof(List<AniDbEpisodeFileXref>))]
    [SwaggerResponse(StatusCodes.Status404NotFound)]
    public Results<Ok<List<AniDbEpisodeFileXref>>, NotFound> ByEpisodeId([FromRoute] int id)
    {
        if (!context.AniDbEpisodes.Any(e => e.Id == id))
            return TypedResults.NotFound();
        return TypedResults.Ok(context.AniDbEpisodeFileXrefs.AsNoTracking().Where(xref => xref.AniDbEpisodeId == id).ToList());
    }

    [HttpGet("[action]/{id:int}")]
    [SwaggerResponse(StatusCodes.Status200OK, type: typeof(List<AniDbEpisodeFileXref>))]
    [SwaggerResponse(StatusCodes.Status404NotFound)]
    public Results<Ok<List<AniDbEpisodeFileXref>>, NotFound> ByAniDbFileId([FromRoute] int id)
    {
        if (!context.AniDbFiles.Any(e => e.Id == id))
            return TypedResults.NotFound();
        return TypedResults.Ok(context.AniDbEpisodeFileXrefs.AsNoTracking().Where(xref => xref.AniDbFileId == id).ToList());
    }

    [HttpGet("[action]/{id:int}")]
    [SwaggerResponse(StatusCodes.Status200OK, type: typeof(List<AniDbEpisodeFileXref>))]
    [SwaggerResponse(StatusCodes.Status404NotFound)]
    public Results<Ok<List<AniDbEpisodeFileXref>>, NotFound> ByAniDbAnimeId([FromRoute] int id)
    {
        if (!context.AniDbAnimes.Any(e => e.Id == id))
            return TypedResults.NotFound();
        return TypedResults.Ok(context.AniDbEpisodeFileXrefs.AsNoTracking().Where(xref => xref.AniDbEpisode.AniDbAnimeId == id).ToList());
    }
}
