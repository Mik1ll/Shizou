using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Shizou.Data.Database;
using Shizou.Data.Models;
using Shizou.Server.Services;
using Swashbuckle.AspNetCore.Annotations;

namespace Shizou.Server.Controllers;

public class AniDbFilesController : EntityGetController<AniDbFile>
{
    private readonly WatchStateService _watchStateService;

    public AniDbFilesController(ILogger<AniDbFilesController> logger, ShizouContext context, WatchStateService watchStateService) : base(logger, context,
        file => file.Id)
    {
        _watchStateService = watchStateService;
    }

    [HttpPut("{fileId}/MarkWatched")]
    [SwaggerResponse(StatusCodes.Status200OK)]
    [SwaggerResponse(StatusCodes.Status404NotFound)]
    public ActionResult MarkWatched(int fileId)
    {
        return _watchStateService.MarkFile(fileId, true) ? Ok() : NotFound();
    }

    [HttpPut("{fileId}/MarkUnwatched")]
    [SwaggerResponse(StatusCodes.Status200OK)]
    [SwaggerResponse(StatusCodes.Status404NotFound)]
    public ActionResult MarkUnwatched(int fileId)
    {
        return _watchStateService.MarkFile(fileId, false) ? Ok() : NotFound();
    }
}
