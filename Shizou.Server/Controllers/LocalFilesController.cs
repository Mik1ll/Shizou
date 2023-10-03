using Microsoft.Extensions.Logging;
using Shizou.Data.Database;
using Shizou.Data.Models;

namespace Shizou.Server.Controllers;

public class LocalFilesController : EntityGetController<LocalFile>
{
    public LocalFilesController(ILogger<LocalFilesController> logger, ShizouContext context) : base(logger, context, file => file.Id)
    {
    }
}
