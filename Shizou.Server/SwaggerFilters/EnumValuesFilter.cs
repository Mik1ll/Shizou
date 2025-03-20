// Based on Unchase.Swashbuckle.AspNetCore.Extensions/Filters/XEnumNamesSchemaFilter.cs

using System;
using System.Linq;
using System.Reflection;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Shizou.Server.SwaggerFilters;

public class EnumValuesFilter : ISchemaFilter
{
    private readonly string _xEnumNamesAlias = "x-enumNames";
    private readonly string _xEnumDescriptionsAlias = "x-enumDescriptions";

    public void Apply(OpenApiSchema schema, SchemaFilterContext context)
    {
        var typeInfo = context.Type.GetTypeInfo();
        var enumsArray = new OpenApiArray();
        var enumsDescriptionsArray = new OpenApiArray();


        if (typeInfo.IsEnum)
        {
            var schemaId = context.Type.Name;
            if (!context.SchemaRepository.Schemas.ContainsKey(schemaId))
            {
                var oldschema = schema;
                schema = new OpenApiSchema(schema);
                oldschema.Reference = new OpenApiReference() { Id = schemaId, Type = ReferenceType.Schema };
                context.SchemaRepository.RegisterType(context.Type, schemaId);
                context.SchemaRepository.AddDefinition(schemaId, schema);
            }
            else
            {
                schema.Reference = new OpenApiReference() { Id = schemaId, Type = ReferenceType.Schema };
                return;
            }

            var names = Enum
                .GetNames(context.Type)
                .Select(name => (Enum.Parse(context.Type, name), new OpenApiString(name)))
                .GroupBy(x => x.Item1)
                .Select(x => x.LastOrDefault().Item2)
                .ToList();
            enumsArray.AddRange(names);
            if (!schema.Extensions.ContainsKey(_xEnumNamesAlias) && enumsArray.Any()) schema.Extensions.Add(_xEnumNamesAlias, enumsArray);

            enumsDescriptionsArray.AddRange(names);
            if (!schema.Extensions.ContainsKey(_xEnumDescriptionsAlias) && enumsDescriptionsArray.Any())
                schema.Extensions.Add(_xEnumDescriptionsAlias, enumsDescriptionsArray);

            return;
        }

        // add "x-enumNames" or its alias for schema with generic types
        if (typeInfo.IsGenericType && !schema.Extensions.ContainsKey(_xEnumNamesAlias))
            foreach (var genericArgumentType in typeInfo.GetGenericArguments())
                if (genericArgumentType.IsEnum)
                    if (schema.Properties?.Count > 0)
                        foreach (var schemaProperty in schema.Properties)
                        {
                            var schemaPropertyValue = schemaProperty.Value;
                            var propertySchema = context.SchemaRepository.Schemas
                                .FirstOrDefault(s => schemaPropertyValue.AllOf.FirstOrDefault(a => a.Reference.Id == s.Key) != null).Value;
                            if (propertySchema != null)
                            {
                                var names = Enum
                                    .GetNames(genericArgumentType)
                                    .Select(name => (Enum.Parse(genericArgumentType, name), new OpenApiString(name)))
                                    .GroupBy(x => x.Item1)
                                    .Select(x => x.LastOrDefault().Item2)
                                    .ToList();
                                enumsArray.AddRange(names);
                                if (!schemaPropertyValue.Extensions.ContainsKey(_xEnumNamesAlias) && enumsArray.Any())
                                    schemaPropertyValue.Extensions.Add(_xEnumNamesAlias, enumsArray);

                                enumsDescriptionsArray.AddRange(names);
                                if (!schemaPropertyValue.Extensions.ContainsKey(_xEnumDescriptionsAlias) && enumsDescriptionsArray.Any())
                                    schemaPropertyValue.Extensions.Add(_xEnumDescriptionsAlias, enumsDescriptionsArray);
                            }
                        }
    }
}
