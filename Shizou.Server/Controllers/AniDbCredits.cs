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

    [HttpGet("[action]/{id}")]
    [SwaggerResponse(StatusCodes.Status200OK, type: typeof(List<AniDbCredit>))]
    [Produces("application/json")]
    public Ok<List<AniDbCredit>> ByAniDbAnimeId(int id)
    {
        var result = _context.AniDbCredits.AsNoTracking()
            .Include(c => c.AniDbCreator)
            .Include(c => c.AniDbCharacter)
            .Where(c => c.AniDbAnimeId == id).ToList();

        return TypedResults.Ok(result);
    }
}
