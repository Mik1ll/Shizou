using System.Linq;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Shizou.SwaggerFilters
{
    public class ClearContentTypeFilter : IDocumentFilter
    {
        public void Apply(OpenApiDocument swaggerDoc, DocumentFilterContext context)
        {
            foreach (var responses in swaggerDoc.Paths.SelectMany(p => p.Value.Operations.Values.Select(o => o.Responses)))
                foreach (var code in new[] {"404", "409"})
                    if (responses.TryGetValue(code, out var response))
                        response.Content.Clear();
            foreach (var path in swaggerDoc.Paths.SelectMany(p => p.Value.Operations).Where(p => p.Key != OperationType.Patch))
                if (path.Value.RequestBody is not null)
                    foreach (var item in path.Value.RequestBody.Content.Where(c => c.Key != "application/json"))
                        path.Value.RequestBody.Content.Remove(item);
        }

        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            foreach (var code in new[] {"404", "409"})
                if (operation.Responses.TryGetValue(code, out var response))
                    response.Content.Clear();
            if (operation.RequestBody is not null)
                foreach (var content in operation.RequestBody.Content.Where(k => k.Key != "application/json" && k.Key != "application/json-patch+json"))
                    operation.RequestBody.Content.Remove(content);
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
