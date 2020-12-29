using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Shizou.Entities;
using Shizou.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Shizou.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class ImportFolderController : ControllerBase
    {
        private readonly ILogger<ImportFolderController> _logger;
        private readonly IImportFolderService _importFolderService;

        public ImportFolderController(ILogger<ImportFolderController> logger, IImportFolderService importFolderService)
        {
            _logger = logger;
            _importFolderService = importFolderService;
        }

        [HttpGet]
        public ActionResult<IEnumerable<ImportFolder>> GetAll()
        {
            return Ok(_importFolderService.GetAll());
        }

        [HttpGet("{id}")]
        public ActionResult<ImportFolder> Get(long id)
        {
            return Ok(_importFolderService.Get(id));
        }

        [HttpGet("location/{location}")]
        public ActionResult<ImportFolder> GetByLocation(string location)
        {
            return Ok(_importFolderService.GetByLocation(location));
        }

        [HttpPost]
        public ActionResult Save([FromBody] ImportFolder importFolder)
        {
            var loc = _importFolderService.Save(importFolder);
            var path = new Uri(@$"{Request.Scheme}://{Request.Host.ToUriComponent()}{Request.Path}/{loc}");
            return Created(path, null);
        }

        [HttpDelete]
        public ActionResult Delete(long id)
        {
            _importFolderService.Delete(id);
            return NoContent();
        }
    }
}
