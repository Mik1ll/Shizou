using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Shizou.Options;
using Shizou.SwaggerFilters;
using Swashbuckle.AspNetCore.Filters;

namespace Shizou.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class OptionsController : ControllerBase
    {
        private readonly ShizouOptions _options;

        public OptionsController(IOptionsSnapshot<ShizouOptions> options)
        {
            _options = options.Value;
        }

        /// <summary>
        ///     Gets current settings.
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public ActionResult<ShizouOptions> Get()
        {
            return Ok(_options);
        }

        /// <summary>
        ///     Overwrites all settings.
        /// </summary>
        /// <param name="options"></param>
        /// <returns></returns>
        [HttpPut]
        public ActionResult Save([FromBody] ShizouOptions options)
        {
            ShizouOptions.SaveSettingsToFile(options);
            return Ok();
        }

        /// <summary>
        ///     Change settings individually
        /// </summary>
        /// <param name="patch"></param>
        /// <returns></returns>
        [HttpPatch]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [SwaggerRequestExample(typeof(JsonPatchOperation), typeof(JsonPatchExample))]
        public ActionResult Patch([FromBody] JsonPatchDocument<ShizouOptions> patch)
        {
            patch.ApplyTo(_options, ModelState);
            if (!ModelState.IsValid)
                return BadRequest(from state in ModelState.Values
                    from error in state.Errors
                    select error.ErrorMessage);
            ShizouOptions.SaveSettingsToFile(_options);
            return Ok();
        }
    }
}
