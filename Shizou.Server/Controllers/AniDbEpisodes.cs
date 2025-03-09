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
public class AniDbEpisodes : EntityGetController<AniDbEpisode>
{
    public AniDbEpisodes(IShizouContext context) : base(context, episode => episode.Id)
    {
    }

    [HttpGet("[action]/{id}")]
    [SwaggerResponse(StatusCodes.Status200OK, type: typeof(List<AniDbEpisode>))]
    [SwaggerResponse(StatusCodes.Status404NotFound)]
    [Produces("application/json")]
    public Results<Ok<List<AniDbEpisode>>, NotFound> ByAniDbFileId(int id)
    {
        if (!Context.AniDbFiles.Any(f => f.Id == id))
            return TypedResults.NotFound();
        var result = DbSet.AsNoTracking().Where(e => e.AniDbEpisodeFileXrefs.Any(xref => xref.AniDbFileId == id)).ToList();
        return TypedResults.Ok(result);
    }

    [HttpGet("[action]/{id}")]
    [SwaggerResponse(StatusCodes.Status200OK, type: typeof(List<AniDbEpisode>))]
    [SwaggerResponse(StatusCodes.Status404NotFound)]
    [Produces("application/json")]
    public Results<Ok<List<AniDbEpisode>>, NotFound> ByAniDbAnimeId(int id)
    {
        if (!Context.AniDbAnimes.Any(a => a.Id == id))
            return TypedResults.NotFound();
        return TypedResults.Ok(DbSet.AsNoTracking().Where(e => e.AniDbAnimeId == id).ToList());
    }
}
