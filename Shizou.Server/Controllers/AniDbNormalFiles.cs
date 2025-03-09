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
public class AniDbNormalFiles : EntityGetController<AniDbNormalFile>
{
    public AniDbNormalFiles(IShizouContext context) : base(context, file => file.Id)
    {
    }

    [HttpGet("[action]/{id}")]
    [SwaggerResponse(StatusCodes.Status200OK, type: typeof(List<AniDbNormalFile>))]
    [SwaggerResponse(StatusCodes.Status404NotFound)]
    [Produces("application/json")]
    public Results<Ok<List<AniDbNormalFile>>, NotFound> ByAniDbAnimeId(int id)
    {
        if (!Context.AniDbAnimes.Any(a => a.Id == id))
            return TypedResults.NotFound();
        return TypedResults.Ok(DbSet.AsNoTracking().Where(f => f.AniDbEpisodes.Any(e => e.AniDbAnimeId == id)).ToList());
    }
}
