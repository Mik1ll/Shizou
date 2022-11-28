using System;
using System.IO;
using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
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
        [HttpGet("{localFileId:int}")]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status416RangeNotSatisfiable)]
        [ProducesResponseType(StatusCodes.Status206PartialContent)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [Produces("application/octet-stream", Type = typeof(FileStreamResult))]
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

            return File(fileStream, mimeType, localFile.Name, true);
        }

        [HttpGet("{localFileId:int}/play")]
        [Produces("text/html")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public ActionResult BrowserPlay(int localFileId)
        {
            _logger.LogInformation("Playing file {localFileId} in browser", localFileId);
            var localDbFile = _context.LocalFiles.Include(e => e.ImportFolder).FirstOrDefault(e => e.Id == localFileId);
            if (localDbFile is null)
                return NotFound();
            return Content(@$"<!DOCTYPE html><html><body>
<video controls
src=""{HttpContext.Request.GetEncodedUrl().Remove(HttpContext.Request.GetEncodedUrl().IndexOf("/play", StringComparison.Ordinal))}""></video>
</body></html>
", "text/html");
        }
    }
}
