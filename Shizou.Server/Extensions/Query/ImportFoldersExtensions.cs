using System.Linq;
using Shizou.Data.Database;
using Shizou.Data.Models;

namespace Shizou.Server.Extensions.Query;

public static class ImportFoldersExtensions
{
    public static ImportFolder? ImportFolderByPath(this ShizouContext context, string path)
    {
        return context.ImportFolders.OrderByDescending(i => i.Path.Length).FirstOrDefault(i => path.StartsWith(i.Path));
    }
}
