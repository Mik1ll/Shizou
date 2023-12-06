using System.Runtime.InteropServices;

namespace Shizou.Data;

public static class FilePaths
{
    public static readonly string ApplicationDataDir = GetApplicationDataDir();

    public static readonly string DatabasePath = Path.Combine(ApplicationDataDir, "ShizouDB.sqlite3");
    public static readonly string TempFileDir = Path.Combine(ApplicationDataDir, "Temp");
    public static readonly string HttpCacheDir = Path.Combine(ApplicationDataDir, "HTTPAnime");
    public static readonly string MyListPath = Path.Combine(ApplicationDataDir, "AniDbMyList.xml");
    public static readonly string MyListBackupDir = Path.Combine(ApplicationDataDir, "MyListBackup");
    public static readonly string OptionsPath = Path.Combine(ApplicationDataDir, "shizou-settings.json");
    public static readonly string LogsDir = Path.Combine(ApplicationDataDir, "Logs");
    public static readonly string ImagesDir = Path.Combine(ApplicationDataDir, "Images");
    public static readonly string AnimePostersDir = Path.Combine(ImagesDir, "AnimePosters");
    public static readonly string AnimeTitlesPath = Path.Combine(ApplicationDataDir, "AnimeTitles.dat");
    public static readonly string ExtraFileDataDir = Path.Combine(ApplicationDataDir, "ExtraFileData");

    // ReSharper disable once InconsistentNaming
    public static string ExtraFileDataSubDir(string ed2k)
    {
        return Path.Combine(ExtraFileDataDir, ed2k);
    }

    private static string GetApplicationDataDir()
    {
        var appdata = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        if (appdata == string.Empty)
        {
            var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            if (string.IsNullOrWhiteSpace(home))
                throw new ArgumentException("Home folder was not found");
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                appdata = Path.Combine(home, ".config");
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                appdata = Path.Combine(home, "Library", "Application Support");
        }

        if (string.IsNullOrWhiteSpace(appdata))
            throw new ArgumentException("App data folder was not found");
        return Path.Combine(appdata, "Shizou");
    }
}
