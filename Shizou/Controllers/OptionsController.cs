using System.Text.Json;
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