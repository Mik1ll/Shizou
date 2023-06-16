using Microsoft.Extensions.Logging;
using Shizou.Data.Database;
using Shizou.Data.Models;

namespace Shizou.Server.Controllers;

public class AniDbEpisodesController : EntityController<AniDbEpisode>
{
    public AniDbEpisodesController(ILogger<AniDbEpisodesController> logger, ShizouContext context) : base(logger, context)
    {
    }
}
