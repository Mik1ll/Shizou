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
public class AniDbCredits : ControllerBase
{
    private readonly IShizouContext _context;

    public AniDbCredits(IShizouContext context) => _context = context;

    [HttpGet("[action]/{id:int}")]
    [SwaggerResponse(StatusCodes.Status200OK, type: typeof(List<AniDbCredit>))]
    [SwaggerResponse(StatusCodes.Status404NotFound)]
    public Results<Ok<List<AniDbCredit>>, NotFound> ByAniDbAnimeId([FromRoute] int id)
    {
        if (!_context.AniDbAnimes.Any(a => a.Id == id))
            return TypedResults.NotFound();

        return TypedResults.Ok(_context.AniDbCredits.AsNoTracking()
            .Include(c => c.AniDbCreator)
            .Include(c => c.AniDbCharacter)
            .Where(c => c.AniDbAnimeId == id).ToList());
    }
}
