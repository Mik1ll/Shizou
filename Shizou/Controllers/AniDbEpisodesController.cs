using AutoMapper;
using Microsoft.Extensions.Logging;
using Shizou.Database;
using Shizou.Dtos;
using Shizou.Models;

namespace Shizou.Controllers;

public class AniDbEpisodesController : EntityController<AniDbEpisode, AniDbEpisodeDto>
{
    public AniDbEpisodesController(ILogger<AniDbEpisodesController> logger, ShizouContext context, IMapper mapper) : base(logger, context, mapper)
    {
    }
}
