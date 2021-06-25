using System.Linq;
using Microsoft.EntityFrameworkCore;
using Shizou.Entities;

namespace Shizou.Extensions
{
    public static class ImportFolderExtensions
    {
        public static ImportFolder? GetByPath(this DbSet<ImportFolder> importFolders, string path)
        {
            return importFolders.OrderByDescending(i => i.Location.Length).SingleOrDefault(i => i.Location.StartsWith(path));
        }
    }
}
