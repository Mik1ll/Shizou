using AutoMapper;
using Microsoft.Extensions.Logging;
using ShizouContracts.Dtos;
using ShizouData.Database;
using ShizouData.Models;

namespace Shizou.Controllers;

public class AniDbFilesController : EntityController<AniDbFile, AniDbFileDto>
{
    public AniDbFilesController(ILogger<AniDbFilesController> logger, ShizouContext context, IMapper mapper) : base(logger, context, mapper)
    {
    }
}