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
public class AniDbAnimes : EntityGetController<AniDbAnime>
{
    private readonly IAnimeTitleSearchService _animeTitleSearchService;

    public AniDbAnimes(IShizouContext context, IAnimeTitleSearchService animeTitleSearchService
    ) : base(context, anime => anime.Id) =>
        _animeTitleSearchService = animeTitleSearchService;


    [HttpGet("[action]")]
    [SwaggerResponse(StatusCodes.Status200OK, type: typeof(List<int>), contentTypes: "application/json")]
    [SwaggerResponse(StatusCodes.Status404NotFound)]
    public async Task<Results<Ok<List<int>>, NotFound>> GetAnimeSearch([FromQuery] string query)
    {
        var result = await _animeTitleSearchService.SearchAsync(query).ConfigureAwait(false);
        if (result is not null)
            return TypedResults.Ok(result.Select(r => r.Item1).ToList());

        return TypedResults.NotFound();
    }
}
