using System.Collections.Generic;
using System.Linq;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.Filters;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Shizou.SwaggerDocumentFilters
{
    // ReSharper disable once ClassNeverInstantiated.Global
    public class JsonPatchDocumentFilter : IDocumentFilter
    {
        public void Apply(OpenApiDocument swaggerDoc, DocumentFilterContext context)
        {
            // Remove irrelevent schemas
            var schemas = swaggerDoc.Components.Schemas.ToList();
            foreach (var item in schemas.Where(item =>
                item.Key.Contains("Operation") || item.Key.EndsWith("JsonPatchDocument") || item.Key == "IContractResolver"))
                swaggerDoc.Components.Schemas.Remove(item.Key);

            // Add JsonPatchOperation schema
            swaggerDoc.Components.Schemas.Add("JsonPatchOperation", new OpenApiSchema
            {
                Title = "JsonPatchOperation",
                Type = "object",
                Properties = new Dictionary<string, OpenApiSchema>
                {
                    {"op", new OpenApiSchema {Type = "string"}},
                    {"from", new OpenApiSchema {Type = "string", Nullable = true}},
                    {"path", new OpenApiSchema {Type = "string"}},
                    {"value", new OpenApiSchema {Type = "object", Nullable = true}}
                }
            });

            // Add corrected JsonPatchDocument Schema
            swaggerDoc.Components.Schemas.Add("JsonPatchDocument", new OpenApiSchema
            {
                Title = "JsonPatchDocument",
                Description = "Array of operations to perform",
                Type = "array",
                Items = new OpenApiSchema
                {
                    Reference = new OpenApiReference {Id = "JsonPatchOperation", Type = ReferenceType.Schema}
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

    public class JsonPatchOperationFilter : IOperationFilter
    {
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            if (operation.OperationId == "Options_PATCH")
                operation.Parameters = new List<OpenApiParameter>
                {
                    new()
                    {
                        Name = "op",
                        In = ParameterLocation.Query,
                        Required = true,
                        Description = "replace, add, copy, move, remove, test"
                    },
                    new()
                    {
                        Name = "path",
                        In = ParameterLocation.Query,
                        Required = true,
                        Description = "path to attribute"
                    },
                    new()
                    {
                        Name = "from",
                        In = ParameterLocation.Query,
                        Required = false,
                        Description = "move and copy only"
                    },
                    new()
                    {
                        Name = "value",
                        In = ParameterLocation.Query,
                        Required = false,
                        Description = "add, replace, test only"
                    }
                };
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
