using System.IO;
using Shizou.Data.Models;

namespace Shizou.Server.Extensions;

public static class LocalFileExtensions
{
    public static bool IsMissing(this LocalFile localFile)
    {
        return localFile.ImportFolder is null || !File.Exists(Path.Combine(localFile.ImportFolder.Path, localFile.PathTail));
    }
}
