using AutoMapper;
using Microsoft.Extensions.Logging;
using Shizou.Contracts.Dtos;
using Shizou.Data.Database;
using Shizou.Data.Models;

namespace Shizou.Server.Controllers;

public class AniDbEpisodesController : EntityController<AniDbEpisode, AniDbEpisodeDto>
{
    public AniDbEpisodesController(ILogger<AniDbEpisodesController> logger, ShizouContext context, IMapper mapper) : base(logger, context, mapper)
    {
    }
}
