using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Shizou.Server.SwaggerFilters;

// ReSharper disable once ClassNeverInstantiated.Global
public class SecurityOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        if (context.MethodInfo.ReflectedType?.GetTypeInfo().GetCustomAttributes<AllowAnonymousAttribute>().Any() ?? false)
            return;

        if (context.MethodInfo.GetCustomAttributes<AllowAnonymousAttribute>().Any())
            return;

        if (context.ApiDescription.ActionDescriptor.EndpointMetadata.OfType<AllowAnonymousAttribute>().Any())
            return;

        if (context.MethodInfo.ReflectedType?.Name == "PwaController")
            return;


        if (!operation.Responses.ContainsKey("401"))
            operation.Responses.Add("401", new OpenApiResponse { Description = "Unauthorized" });

        // if (!operation.Security.Any(requirement => requirement.Any(scheme => scheme.Key.Reference.Id == Constants.IdentityCookieName)))
        //     operation.Security = new List<OpenApiSecurityRequirement>
        //     {
        //         new()
        //         {
        //             {
        //                 new OpenApiSecurityScheme
        //                 {
        //                     Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = Constants.IdentityCookieName }
        //                 },
        //                 new List<string>()
        //             }
        //         }
        //     };
    }
}
