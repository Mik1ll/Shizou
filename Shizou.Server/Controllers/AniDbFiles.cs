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
public class AniDbFiles : EntityGetController<AniDbFile>
{
    private readonly WatchStateService _watchStateService;

    public AniDbFiles(IShizouContext context, WatchStateService watchStateService) : base(context,
        file => file.Id)
    {
        _watchStateService = watchStateService;
    }

    [HttpPut("{id:int}/MarkWatched")]
    [SwaggerResponse(StatusCodes.Status200OK)]
    [SwaggerResponse(StatusCodes.Status404NotFound)]
    public Results<Ok, NotFound> MarkWatched([FromRoute] int id) => _watchStateService.MarkFile(id, true) ? TypedResults.Ok() : TypedResults.NotFound();

    [HttpPut("{id:int}/MarkUnwatched")]
    [SwaggerResponse(StatusCodes.Status200OK)]
    [SwaggerResponse(StatusCodes.Status404NotFound)]
    public Results<Ok, NotFound> MarkUnwatched([FromRoute] int id) => _watchStateService.MarkFile(id, false) ? TypedResults.Ok() : TypedResults.NotFound();

    [HttpGet("[action]/{id:int}")]
    [SwaggerResponse(StatusCodes.Status200OK, type: typeof(List<AniDbFile>))]
    [SwaggerResponse(StatusCodes.Status404NotFound)]
    public Results<Ok<List<AniDbFile>>, NotFound> ByAniDbAnimeId([FromRoute] int id)
    {
        if (!Context.AniDbAnimes.Any(a => a.Id == id))
            return TypedResults.NotFound();
        return TypedResults.Ok(DbSet.AsNoTracking().Where(f => f.AniDbEpisodes.Any(e => e.AniDbAnimeId == id)).ToList());
    }
}
