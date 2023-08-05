using Shizou.Data;

namespace Shizou.Blazor;

public static class WebPaths
{
    public static readonly string ImagesDir = GetWebPath(FilePaths.ImagesDir);
    public static readonly string AnimePostersDir = GetWebPath(FilePaths.AnimePostersDir);

    public static string GetWebPath(string filepath)
    {
        return $"/{Path.GetRelativePath(FilePaths.ApplicationDataDir, filepath)
            .Replace('\\', '/')}";
    }
}
