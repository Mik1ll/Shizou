namespace ShizouData;

public static class FilePaths
{
    public static readonly string ApplicationDataDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Shizou");
    public static readonly string TempFileDir = Path.Combine(ApplicationDataDir, "Temp");
    public static readonly string HttpCacheDir = Path.Combine(ApplicationDataDir, "HTTPAnime");
    public static readonly string MyListPath = Path.Combine(ApplicationDataDir, "AniDbMyList.xml");
    public static readonly string MyListBackupDir = Path.Combine(ApplicationDataDir, "MyListBackup");
    public static readonly string OptionsPath = Path.Combine(ApplicationDataDir, "shizou-settings.json");
    public static readonly string LogsDir = Path.Combine(ApplicationDataDir, "Logs");
}
