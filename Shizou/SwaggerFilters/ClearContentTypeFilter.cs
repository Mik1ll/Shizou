using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Shizou.SwaggerFilters
{
    public class ClearContentTypeFilter : IOperationFilter
    {
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            if (operation.Responses.TryGetValue("404", out var response))
                response.Content.Clear();

            // var data = new OpenApiResponse
            // {
            //     Description = "Ok",
            //     Content = new Dictionary<string, OpenApiMediaType>
            //     {
            //         ["application/json"] = new(),
            //         ["application/xml"] = new(),
            //     }
            // };
            //
            // operation.Responses.Add("200", data);
        }
    }
}
