using Microsoft.AspNetCore.Mvc;
using Shizou.Data;
using Shizou.Data.Database;
using Shizou.Data.Models;

namespace Shizou.Server.Controllers;

[ApiController]
[Route($"{Constants.ApiPrefix}/[controller]")]
public class FileWatchedStates : EntityController<FileWatchedState>
{
    public FileWatchedStates(IShizouContext context) : base(context, state => state.AniDbFileId)
    {
    }
}
