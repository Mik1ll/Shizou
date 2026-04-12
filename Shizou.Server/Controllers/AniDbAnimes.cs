using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Shizou.Data;
using Shizou.Data.Database;
using Shizou.Data.Models;
using Shizou.Server.Services;
using Swashbuckle.AspNetCore.Annotations;

namespace Shizou.Server.Controllers;

[ApiController]
[Route($"{Constants.ApiPrefix}/[controller]")]
public class AniDbAnimes(IShizouContext context, IAnimeTitleSearchService animeTitleSearchService) : ControllerBase
{
    [HttpGet]
    [SwaggerResponse(StatusCodes.Status200OK, type: typeof(List<AniDbAnime>))]
    public Ok<List<AniDbAnime>> GetAll() => EntityEndpoints.GetAll(context.AniDbAnimes);

    [HttpGet("{id:int}")]
    [SwaggerResponse(StatusCodes.Status200OK, type: typeof(AniDbAnime))]
    [SwaggerResponse(StatusCodes.Status404NotFound)]
    public Results<Ok<AniDbAnime>, NotFound> Get([FromRoute] int id)
        => EntityEndpoints.GetById(context.AniDbAnimes, e => e.Id == id);

    [HttpGet("[action]")]
    [SwaggerResponse(StatusCodes.Status200OK, type: typeof(List<int>))]
    [SwaggerResponse(StatusCodes.Status404NotFound)]
    public async Task<Results<Ok<List<int>>, NotFound>> GetAnimeSearch([FromQuery] string query)
    {
        var result = await animeTitleSearchService.SearchAsync(query).ConfigureAwait(false);
        if (result is not null)
            return TypedResults.Ok(result.Select(r => r.Item1).ToList());

        return TypedResults.NotFound();
    }
}
