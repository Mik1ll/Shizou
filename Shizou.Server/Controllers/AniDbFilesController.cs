using Microsoft.Extensions.Logging;
using Shizou.Data.Database;
using Shizou.Data.Models;

namespace Shizou.Server.Controllers;

public class AniDbFilesController : EntityGetController<AniDbFile>
{
    public AniDbFilesController(ILogger<AniDbFilesController> logger, ShizouContext context) : base(logger, context)
    {
    }
}
