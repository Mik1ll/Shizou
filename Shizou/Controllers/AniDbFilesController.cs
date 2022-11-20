using Microsoft.Extensions.Logging;
using Shizou.Database;
using Shizou.Models;

namespace Shizou.Controllers
{
    public class AniDbFilesController : EntityController<AniDbFile>
    {
        public AniDbFilesController(ILogger<AniDbFilesController> logger, ShizouContext context) : base(logger, context)
        {
        }
    }
}
