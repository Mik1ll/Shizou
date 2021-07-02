using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Shizou.Database;
using Shizou.Entities;

namespace Shizou.Controllers
{
    public class AniDbFilesController : EntityController<AniDbFile>
    {
        public AniDbFilesController(ILogger<AniDbFilesController> logger, ShizouContext context) : base(logger, context)
        {
        }

        public override ActionResult<IQueryable<AniDbFile>> List()
        {
            return Ok(Context.AniDbFiles.Include(e => e.Subtitles).Include(e => e.Audio).Include(e => e.Video));
        }
    }
}
