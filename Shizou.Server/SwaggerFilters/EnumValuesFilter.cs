using System.Reflection;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Shizou.Server.SwaggerFilters;

public class EnumValuesFilter : ISchemaFilter
{
    public void Apply(OpenApiSchema schema, SchemaFilterContext context)
    {
        var typeInfo = context.Type.GetTypeInfo();
        if (typeInfo.IsEnum)
        {
            var schemaId = context.Type.Name;
            if (context.SchemaRepository.TryLookupByType(context.Type, out var referenceSchema))
            {
                schema.Reference = referenceSchema.Reference;
            }
            else
            {
                context.SchemaRepository.RegisterType(context.Type, schemaId);
                schema.Reference = context.SchemaRepository.AddDefinition(schemaId, new OpenApiSchema(schema)).Reference;
            }
        }
    }
}
