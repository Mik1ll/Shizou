using System.Linq;
using Shizou.Data.Models;

namespace Shizou.Server.Extensions;

public static class ImportFolderExtensions
{
    public static ImportFolder? GetByPath(this IQueryable<ImportFolder> query, string path)
    {
        return query.OrderByDescending(i => i.Path.Length).FirstOrDefault(i => path.StartsWith(i.Path));
    }
}