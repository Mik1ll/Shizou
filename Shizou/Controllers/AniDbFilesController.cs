using AutoMapper;
using Microsoft.Extensions.Logging;
using Shizou.Database;
using Shizou.Dtos;
using Shizou.Models;

namespace Shizou.Controllers
{
    public class AniDbFilesController : EntityController<AniDbFile, AniDbFileDto>
    {
        public AniDbFilesController(ILogger<AniDbFilesController> logger, ShizouContext context, IMapper mapper) : base(logger, context, mapper)
        {
        }
    }
}
