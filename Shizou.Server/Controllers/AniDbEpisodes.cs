using Microsoft.AspNetCore.Mvc;
using Shizou.Data;
using Shizou.Data.Database;
using Shizou.Data.Models;

namespace Shizou.Server.Controllers;

[ApiController]
[Route($"{Constants.ApiPrefix}/[controller]")]
public class AniDbEpisodes : EntityGetController<AniDbEpisode>
{
    public AniDbEpisodes(IShizouContext context) : base(context, episode => episode.Id)
    {
    }
}
