using Microsoft.Extensions.Logging;
using Shizou.Database;
using Shizou.Models;

namespace Shizou.Controllers;

public class AniDbEpisodesController : EntityController<AniDbEpisode>
{
    public AniDbEpisodesController(ILogger<AniDbEpisodesController> logger, ShizouContext context) : base(logger, context)
    {
    }
}
