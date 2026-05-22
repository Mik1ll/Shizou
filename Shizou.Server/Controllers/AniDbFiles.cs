using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Shizou.Data;
using Shizou.Data.Database;
using Shizou.Data.Models;
using Shizou.Server.Services;
using Swashbuckle.AspNetCore.Annotations;

namespace Shizou.Server.Controllers;

[ApiController]
[Route($"{Constants.ApiPrefix}/[controller]")]
public class AniDbFiles(IShizouContext context, WatchStateService watchStateService) : ControllerBase
{
    [HttpGet]
    [SwaggerResponse(StatusCodes.Status200OK, type: typeof(List<AniDbFile>))]
    public Ok<List<AniDbFile>> GetAll() => EntityEndpoints.GetAll(context.AniDbFiles);

    [HttpGet("{id:int}")]
    [SwaggerResponse(StatusCodes.Status200OK, type: typeof(AniDbFile))]
    [SwaggerResponse(StatusCodes.Status404NotFound)]
    public Results<Ok<AniDbFile>, NotFound> Get([FromRoute] int id)
        => EntityEndpoints.GetById(context.AniDbFiles, e => e.Id == id);

    [HttpPut("{id:int}/MarkWatched")]
    [SwaggerResponse(StatusCodes.Status200OK)]
    [SwaggerResponse(StatusCodes.Status404NotFound)]
    public Results<Ok, NotFound> MarkWatched([FromRoute] int id) => watchStateService.MarkFile(id, true) ? TypedResults.Ok() : TypedResults.NotFound();

    [HttpPut("{id:int}/MarkUnwatched")]
    [SwaggerResponse(StatusCodes.Status200OK)]
    [SwaggerResponse(StatusCodes.Status404NotFound)]
    public Results<Ok, NotFound> MarkUnwatched([FromRoute] int id) => watchStateService.MarkFile(id, false) ? TypedResults.Ok() : TypedResults.NotFound();

    [HttpGet("[action]/{id:int}")]
    [SwaggerResponse(StatusCodes.Status200OK, type: typeof(List<AniDbFile>))]
    [SwaggerResponse(StatusCodes.Status404NotFound)]
    public Results<Ok<List<AniDbFile>>, NotFound> ByAniDbAnimeId([FromRoute] int id)
    {
        if (!context.AniDbAnimes.Any(a => a.Id == id))
            return TypedResults.NotFound();
        return TypedResults.Ok(context.AniDbFiles.AsNoTracking().Where(f => f.AniDbEpisodes.Any(e => e.AniDbAnimeId == id)).ToList());
    }
}
