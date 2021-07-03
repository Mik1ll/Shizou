using Microsoft.Extensions.Logging;
using Shizou.Database;
using Shizou.Dtos;
using Shizou.Entities;

namespace Shizou.Controllers
{
    public class AniDbFilesController : EntityController<AniDbFileDto, AniDbFile>
    {
        public AniDbFilesController(ILogger<AniDbFilesController> logger, ShizouContext context) : base(logger, context)
        {
        }
    }
}
