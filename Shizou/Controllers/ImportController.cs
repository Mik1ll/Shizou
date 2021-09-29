using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Shizou.Services.Import;

namespace Shizou.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class ImportController : ControllerBase
    {
        private readonly ILogger<ImportController> _logger;
        private readonly Importer _importer;

        public ImportController(ILogger<ImportController> logger, Importer importer)
        {
            _logger = logger;
            _importer = importer;
        }

        /// <summary>
        ///     Start import
        /// </summary>
        /// <returns></returns>
        [HttpPut("start")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public ActionResult StartImport()
        {
            _importer.Import();
            return Ok();
        }

        /// <summary>
        ///     Scan an import folder
        /// </summary>
        /// <param name="folderId"></param>
        /// <returns></returns>
        [HttpPut("scan/{folderId}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public ActionResult ScanFolder(int folderId)
        {
            _importer.ScanImportFolder(folderId);
            return Ok();
        }
    }
}
