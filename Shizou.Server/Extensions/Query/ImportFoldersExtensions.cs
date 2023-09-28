using System.Linq;
using Shizou.Data.Models;

namespace Shizou.Server.Extensions.Query;

public static class ImportFoldersExtensions
{
    public static ImportFolder? ByPath(this IQueryable<ImportFolder> query, string path)
    {
        return query.OrderByDescending(i => i.Path.Length).FirstOrDefault(i => path.StartsWith(i.Path));
    }
}
