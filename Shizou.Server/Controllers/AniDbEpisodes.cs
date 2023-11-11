using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Shizou.Data.Database;
using Shizou.Data.Models;

namespace Shizou.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AniDbEpisodes : EntityGetController<AniDbEpisode>
{
    public AniDbEpisodes(ILogger<AniDbEpisodes> logger, ShizouContext context) : base(logger, context, episode => episode.Id)
    {
    }
}
