using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.DependencyInjection;
using Shizou.Data.Database;
using Shizou.Data.Models;

namespace Shizou.Data.Validation;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public sealed class UniqueAttribute : ValidationAttribute
{
    public UniqueAttribute() : base(() => "The {0} field must be unique")
    {
    }

    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        var contextFactory = (IShizouContextFactory)validationContext.GetRequiredService(typeof(IShizouContextFactory));
        using var context = contextFactory.CreateDbContext();
        var unique = validationContext.ObjectInstance switch
        {
            ImportFolder myImportFolder =>
                context.ImportFolders.Where(i => i.Id != myImportFolder.Id) is var otherFolders &&
                validationContext.MemberName switch
                {
                    nameof(ImportFolder.Name) => !otherFolders.Any(i => i.Name == myImportFolder.Name),
                    nameof(ImportFolder.Path) => !otherFolders.Any(i => i.Path == myImportFolder.Path),
                    _ => throw new InvalidOperationException("Unique validation not configured for property")
                },
            _ => throw new InvalidOperationException("Unique validation not configured for class")
        };
        return unique ? ValidationResult.Success : new ValidationResult(null, new[] { validationContext.MemberName });
    }
}
