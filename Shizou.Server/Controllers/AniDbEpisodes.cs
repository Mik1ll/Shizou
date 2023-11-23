using Microsoft.AspNetCore.Mvc;
using Shizou.Data.Database;
using Shizou.Data.Models;

namespace Shizou.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AniDbEpisodes : EntityGetController<AniDbEpisode>
{
    public AniDbEpisodes(IShizouContext context) : base(context, episode => episode.Id)
    {
    }
}
