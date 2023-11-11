using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Shizou.Data.Database;
using Shizou.Data.Models;
using Shizou.Server.Services;
using Swashbuckle.AspNetCore.Annotations;

namespace Shizou.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AniDbFiles : EntityGetController<AniDbFile>
{
    private readonly WatchStateService _watchStateService;

    public AniDbFiles(ILogger<AniDbFiles> logger, ShizouContext context, WatchStateService watchStateService) : base(logger, context,
        file => file.Id)
    {
        _watchStateService = watchStateService;
    }

    [HttpPut("{fileId}/MarkWatched")]
    [SwaggerResponse(StatusCodes.Status200OK)]
    [SwaggerResponse(StatusCodes.Status404NotFound)]
    public Results<Ok, NotFound> MarkWatched(int fileId)
    {
        return _watchStateService.MarkFile(fileId, true) ? TypedResults.Ok() : TypedResults.NotFound();
    }

    [HttpPut("{fileId}/MarkUnwatched")]
    [SwaggerResponse(StatusCodes.Status200OK)]
    [SwaggerResponse(StatusCodes.Status404NotFound)]
    public Results<Ok, NotFound> MarkUnwatched(int fileId)
    {
        return _watchStateService.MarkFile(fileId, false) ? TypedResults.Ok() : TypedResults.NotFound();
    }
}
