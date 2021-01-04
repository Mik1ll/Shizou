using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Shizou.Options;
using Shizou.SwaggerDocumentFilters;
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
        /// Gets current settings.
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [ProducesResponseType(typeof(ShizouOptions), StatusCodes.Status200OK)]
        public ActionResult<ShizouOptions> Get()
        {
            return Ok(_options);
        }

        /// <summary>
        /// Overwrites all settings.
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
        /// Set settings from json
        /// </summary>
        /// <param name="patch"></param>
        /// <returns></returns>
        [HttpPatch]
        [SwaggerRequestExample(typeof(JsonPatchOperation), typeof(JsonPatchExample))]
        public ActionResult PatchShizouOptions([FromBody] JsonPatchDocument<ShizouOptions> patch)
        {
            patch.ApplyTo(_options, ModelState);
            ShizouOptions.SaveSettingsToFile(_options);
            return Ok();
        }
    }
}