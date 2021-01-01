using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.Filters;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Shizou.SwaggerDocumentFilters
{
    public class JsonPatchDocumentFilter : IDocumentFilter
    {
        public void Apply(OpenApiDocument swaggerDoc, DocumentFilterContext context)
        {
            var schemas = swaggerDoc.Components.Schemas.ToList();
            foreach (var item in schemas)
            {
                if (item.Key.Contains("Operation") || item.Key.EndsWith("JsonPatchDocument") || item.Key == "IContractResolver" || item.Key == "ProblemDetails")
                    swaggerDoc.Components.Schemas.Remove(item.Key);
            }
            swaggerDoc.Components.Schemas.Add("JsonPatchOperation", new OpenApiSchema
            {
                Title = "JsonPatchOperation",
                Type = "object",
                Properties = new Dictionary<string, OpenApiSchema>
                {
                    {"op", new OpenApiSchema{ Type = "string" } },
                    {"path", new OpenApiSchema{ Type = "string" } },
                    {"value", new OpenApiSchema{ Type = "object", Nullable = true } }
                }
            });

            swaggerDoc.Components.Schemas.Add("JsonPatchDocument", new OpenApiSchema
            {
                Title = "JsonPatchDocument",
                Description = "Array of operations to perform",
                Type = "array",
                Items = new OpenApiSchema
                {
                    Reference = new OpenApiReference() { Id = "JsonPatchOperation", Type = ReferenceType.Schema }
                }
            });

            foreach (var path in swaggerDoc.Paths.SelectMany(p => p.Value.Operations)
                                                 .Where(p => p.Key == OperationType.Patch))
            {
                foreach (var item in path.Value.RequestBody.Content.Where(c => c.Key != "application/json-patch+json"))
                    path.Value.RequestBody.Content.Remove(item.Key);
                var response = path.Value.RequestBody.Content.Single(c => c.Key == "application/json-patch+json");
                response.Value.Schema = new OpenApiSchema
                {
                    Reference = new OpenApiReference { Type = ReferenceType.Schema, Id = "JsonPatchDocument" }
                };
            }
        }
    }

    public class JsonPatchOperation
    {
        public string Op { get; set; } = string.Empty;

        public string Path { get; set; } = string.Empty;

        public object Value { get; set; } = null!;
    }

    public class JsonPatchExample : IExamplesProvider<JsonPatchOperation[]>
    {
        public JsonPatchOperation[] GetExamples()
        {
            return new[]
            {
                new JsonPatchOperation
                {
                    Op = "add",
                    Path = "AniDB/Username",
                    Value = "Testuser"
                },
                new JsonPatchOperation
                {
                    Op = "replace",
                    Path = "AniDB/ServerPort",
                    Value = 8000
                }
            };
        }
    }
}