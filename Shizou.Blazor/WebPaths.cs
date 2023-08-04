using Shizou.Data;

namespace Shizou.Blazor;

public class WebPaths
{
    public static readonly string ImagesDir = $"/{Path.GetFileName(FilePaths.ImagesDir)}";
    public static readonly string AnimePostersDir = $"{ImagesDir}/{Path.GetRelativePath(FilePaths.ImagesDir, FilePaths.AnimePostersDir)
        .Replace('\\', '/')}";
}
