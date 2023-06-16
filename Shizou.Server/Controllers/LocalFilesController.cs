using Microsoft.Extensions.Logging;
using Shizou.Data.Database;
using Shizou.Data.Models;

namespace Shizou.Server.Controllers;

public class LocalFilesController : EntityController<LocalFile>
{
    public LocalFilesController(ILogger<LocalFilesController> logger, ShizouContext context) : base(logger, context)
    {
    }
}
