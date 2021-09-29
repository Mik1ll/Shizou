using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Shizou.CommandProcessors;

namespace Shizou.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class QueueController : ControllerBase
    {
        private readonly IEnumerable<CommandProcessor> _queues;

        public QueueController(IEnumerable<CommandProcessor> queues)
        {
            _queues = queues;
        }

        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public ActionResult List()
        {
            return Ok(_queues);
        }

        [HttpGet("{queueType}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public ActionResult Get(QueueType queueType)
        {
            return Ok(_queues.Single(q => q.QueueType == queueType));
        }
    }
}
