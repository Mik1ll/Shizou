using AutoMapper;
using Microsoft.Extensions.Logging;
using ShizouContracts.Dtos;
using ShizouData.Database;
using ShizouData.Models;

namespace Shizou.Controllers;

public class AniDbEpisodesController : EntityController<AniDbEpisode, AniDbEpisodeDto>
{
    public AniDbEpisodesController(ILogger<AniDbEpisodesController> logger, ShizouContext context, IMapper mapper) : base(logger, context, mapper)
    {
    }
}
