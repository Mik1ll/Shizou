using Microsoft.AspNetCore.Mvc;
using Shizou.Data;
using Shizou.Data.Database;
using Shizou.Data.Models;

namespace Shizou.Server.Controllers;

[ApiController]
[Route($"{Constants.ApiPrefix}/[controller]")]
public class AniDbAnimes : EntityGetController<AniDbAnime>
{
    public AniDbAnimes(IShizouContext context) : base(context, anime => anime.Id)
    {
    }
}
