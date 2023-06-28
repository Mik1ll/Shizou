﻿using Microsoft.Extensions.Logging;
using Shizou.Data.Database;
using Shizou.Data.Models;

namespace Shizou.Server.Controllers;

public class AniDbFilesController : EntityController<AniDbFile>
{
    public AniDbFilesController(ILogger<AniDbFilesController> logger, ShizouContext context) : base(logger, context)
    {
    }
}