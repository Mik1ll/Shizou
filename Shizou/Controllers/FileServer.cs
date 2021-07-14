using System.IO;
using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Shizou.Database;

namespace Shizou.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class FileServer : ControllerBase
    {
        private readonly ShizouContext _context;
        private readonly ILogger<FileServer> _logger;

        public FileServer(ILogger<FileServer> logger, ShizouContext context)
        {
            _logger = logger;
            _context = context;
        }


        /// <summary>
        ///     Get file by local Id
        /// </summary>
        /// <param name="localFileId"></param>
        /// <returns></returns>
        /// <response code="404">Not Found</response>
        [HttpGet("{localFileId:int}")]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status416RangeNotSatisfiable)]
        [ProducesResponseType(StatusCodes.Status206PartialContent)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public ActionResult Get(int localFileId)
        {
            var localDbFile = _context.LocalFiles.Include(e => e.ImportFolder).FirstOrDefault(e => e.Id == localFileId);
            if (localDbFile is null)
                return NotFound();
            var localFile = new FileInfo(Path.GetFullPath(Path.Combine(localDbFile.ImportFolder.Path, localDbFile.PathTail)));
            if (!localFile.Exists)
                return NotFound();
            if (!new FileExtensionContentTypeProvider().TryGetContentType(localFile.Name, out var mimeType))
                mimeType = "application/octet-stream";
            var fileStream = new FileStream(localFile.FullName, FileMode.Open, FileAccess.Read, FileShare.Read, 1 << 19, FileOptions.Asynchronous);

            return File(fileStream, mimeType, true);
        }
    }
}
