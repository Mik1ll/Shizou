using AutoMapper;
using Microsoft.Extensions.Logging;
using Shizou.Contracts.Dtos;
using Shizou.Data.Database;
using Shizou.Data.Models;

namespace Shizou.Server.Controllers;

public class AniDbFilesController : EntityController<AniDbFile, AniDbFileDto>
{
    public AniDbFilesController(ILogger<AniDbFilesController> logger, ShizouContext context, IMapper mapper) : base(logger, context, mapper)
    {
    }
}