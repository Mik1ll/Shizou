using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Shizou.CommandProcessors;
using Shizou.Database;
using Shizou.Dtos;

namespace Shizou.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class QueueController : ControllerBase
    {
        private readonly IEnumerable<CommandProcessor> _queues;
        private readonly ShizouContext _context;

        public QueueController(IEnumerable<CommandProcessor> queues, ShizouContext context)
        {
            _queues = queues;
            _context = context;
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
            var queue = _queues.FirstOrDefault(q => q.QueueType == queueType);
            return queue is not null ? Ok(queue) : NotFound();
        }

        [HttpPut("{queueType}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public ActionResult Pause(QueueType queueType, bool paused)
        {
            var queue = _queues.FirstOrDefault(q => q.QueueType == queueType);
            if (queue is null) return NotFound();
            queue.Paused = paused;
            if (queue.Paused && !paused)
                return Conflict($"Pause state locked: {queue.PauseReason}");
            return Ok();
        }

        [HttpGet("{queueType}/current")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public ActionResult<CommandRequestDto?> GetCurrentCommand(QueueType queueType)
        {
            var queue = _queues.FirstOrDefault(q => q.QueueType == queueType);
            if (queue is null) return NotFound("Queue not found");
            return Ok(queue.CurrentCommand?.ToDto());
        }

        [HttpGet("{queueType}/queued")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public ActionResult<IEnumerable<CommandRequestDto>> GetQueuedCommands(QueueType queueType)
        {
            var queue = _queues.FirstOrDefault(q => q.QueueType == queueType);
            if (queue is null) return NotFound("Queue not found");
            return Ok(_context.CommandRequests.Where(cr => cr.QueueType == queueType));
        }
    }
}
