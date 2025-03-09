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

    [HttpPut("{fileId}/MarkWatched")]
    [SwaggerResponse(StatusCodes.Status200OK)]
    [SwaggerResponse(StatusCodes.Status404NotFound)]
    public Results<Ok, NotFound> MarkWatched(int fileId)
    {
        return _watchStateService.MarkFile(fileId, true) ? TypedResults.Ok() : TypedResults.NotFound();
    }

    [HttpPut("{fileId}/MarkUnwatched")]
    [SwaggerResponse(StatusCodes.Status200OK)]
    [SwaggerResponse(StatusCodes.Status404NotFound)]
    public Results<Ok, NotFound> MarkUnwatched(int fileId)
    {
        return _watchStateService.MarkFile(fileId, false) ? TypedResults.Ok() : TypedResults.NotFound();
    }

    [HttpGet("[action]/{id}")]
    [SwaggerResponse(StatusCodes.Status200OK, type: typeof(List<AniDbFile>))]
    [SwaggerResponse(StatusCodes.Status404NotFound)]
    [Produces("application/json")]
    public Results<Ok<List<AniDbFile>>, NotFound> ByAniDbAnimeId(int id)
    {
        if (!Context.AniDbAnimes.Any(a => a.Id == id))
            return TypedResults.NotFound();
        return TypedResults.Ok(DbSet.AsNoTracking().Where(f => f.AniDbEpisodes.Any(e => e.AniDbAnimeId == id)).ToList());
    }
}
