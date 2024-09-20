using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Http;
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
    [SwaggerResponse(StatusCodes.Status200OK)]
    [SwaggerResponse(StatusCodes.Status404NotFound)]
    [Produces("application/json")]
    public ActionResult<List<AniDbEpisode>> ByAniDbFileId(int id)
    {
        var result = DbSet.AsNoTracking().Where(e => e.AniDbEpisodeFileXrefs.Any(xref => xref.AniDbFileId == id)).ToList();
        return Ok(result);
    }
}
