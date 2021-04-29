using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.Filters;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Shizou.SwaggerFilters
{
    // ReSharper disable once ClassNeverInstantiated.Global
    public class JsonPatchDocumentFilter : IDocumentFilter
    {
        public void Apply(OpenApiDocument swaggerDoc, DocumentFilterContext context)
        {
            // Remove irrelevent schemas
            var schemas = swaggerDoc.Components.Schemas.ToList();
            foreach (var item in schemas.Where(item =>
                item.Key.Contains("Operation") || item.Key.EndsWith("JsonPatchDocument") || item.Key == "IContractResolver" || item.Key.StartsWith("Edm") ||
                item.Key.StartsWith("IEdm") || item.Key.StartsWith("Odata", StringComparison.OrdinalIgnoreCase) || item.Key.StartsWith("Problem")))
                swaggerDoc.Components.Schemas.Remove(item.Key);

            // Add JsonPatchOperation schema
            swaggerDoc.Components.Schemas.Add(nameof(JsonPatchOperation), new OpenApiSchema
            {
                Title = nameof(JsonPatchOperation),
                Type = "object",
                Properties = new Dictionary<string, OpenApiSchema>
                {
                    {"op", new OpenApiSchema {Type = "string"}},
                    {"from", new OpenApiSchema {Type = "string", Nullable = true}},
                    {"path", new OpenApiSchema {Type = "string"}},
                    {"value", new OpenApiSchema {Type = "object", Nullable = true}}
                }
            });

            swaggerDoc.Components.Schemas.Add("JsonPatchDocument", new OpenApiSchema
            {
                Type = "array",
                Items = new OpenApiSchema
                {
                    Reference = new OpenApiReference {Id = nameof(JsonPatchOperation), Type = ReferenceType.Schema}
                }
            });

            // Replace request schemas for patch operations with the new schema
            foreach (var path in swaggerDoc.Paths.SelectMany(p => p.Value.Operations)
                .Where(p => p.Key == OperationType.Patch))
            {
                foreach (var item in path.Value.RequestBody.Content.Where(c => c.Key != "application/json-patch+json"))
                    path.Value.RequestBody.Content.Remove(item.Key);
                var response = path.Value.RequestBody.Content.Single(c => c.Key == "application/json-patch+json");
                response.Value.Schema = new OpenApiSchema
                {
                    Reference = new OpenApiReference {Type = ReferenceType.Schema, Id = "JsonPatchDocument"}
                };
            }
        }
    }

    public class JsonPatchOperation
    {
        public string Op { get; set; } = string.Empty;

        public string? From { get; set; }

        public string Path { get; set; } = string.Empty;

        public object? Value { get; set; }
    }

    public class JsonPatchExample : IMultipleExamplesProvider<JsonPatchOperation>
    {
        public IEnumerable<SwaggerExample<JsonPatchOperation>> GetExamples()
        {
            yield return SwaggerExample.Create("replace", new JsonPatchOperation
            {
                Op = "replace",
                Path = "AniDB/ServerPort",
                Value = 8000
            });
            yield return SwaggerExample.Create("add", new JsonPatchOperation
            {
                Op = "add",
                Path = "AniDB/Username",
                Value = "Testuser"
            });
        }
    }
}
