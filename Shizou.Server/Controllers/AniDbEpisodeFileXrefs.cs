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
public class AniDbEpisodeFileXrefs : ControllerBase
{
    private readonly IShizouContext _context;
    private readonly IShizouDbSet<AniDbEpisodeFileXref> _dbSet;

    public AniDbEpisodeFileXrefs(IShizouContext context)
    {
        _context = context;
        _dbSet = context.AniDbEpisodeFileXrefs;
    }

    [HttpGet]
    [SwaggerResponse(StatusCodes.Status200OK)]
    public Ok<List<AniDbEpisodeFileXref>> Get() => TypedResults.Ok(_dbSet.AsNoTracking().ToList());

    [HttpGet("[action]/{id:int}")]
    [SwaggerResponse(StatusCodes.Status200OK, type: typeof(List<AniDbEpisodeFileXref>))]
    [SwaggerResponse(StatusCodes.Status404NotFound)]
    public Results<Ok<List<AniDbEpisodeFileXref>>, NotFound> ByEpisodeId([FromRoute] int id)
    {
        if (!_context.AniDbEpisodes.Any(e => e.Id == id))
            return TypedResults.NotFound();
        return TypedResults.Ok(_dbSet.AsNoTracking().Where(xref => xref.AniDbEpisodeId == id).ToList());
    }

    [HttpGet("[action]/{id:int}")]
    [SwaggerResponse(StatusCodes.Status200OK, type: typeof(List<AniDbEpisodeFileXref>))]
    [SwaggerResponse(StatusCodes.Status404NotFound)]
    public Results<Ok<List<AniDbEpisodeFileXref>>, NotFound> ByAniDbFileId([FromRoute] int id)
    {
        if (!_context.AniDbFiles.Any(e => e.Id == id))
            return TypedResults.NotFound();
        return TypedResults.Ok(_dbSet.AsNoTracking().Where(xref => xref.AniDbFileId == id).ToList());
    }

    [HttpGet("[action]/{id:int}")]
    [SwaggerResponse(StatusCodes.Status200OK, type: typeof(List<AniDbEpisodeFileXref>))]
    [SwaggerResponse(StatusCodes.Status404NotFound)]
    public Results<Ok<List<AniDbEpisodeFileXref>>, NotFound> ByAniDbAnimeId([FromRoute] int id)
    {
        if (!_context.AniDbAnimes.Any(e => e.Id == id))
            return TypedResults.NotFound();
        return TypedResults.Ok(_dbSet.AsNoTracking().Where(xref => xref.AniDbEpisode.AniDbAnimeId == id).ToList());
    }
}
