using System.Runtime.InteropServices;

namespace Shizou.Data;

public static class FilePaths
{
    public static readonly string ApplicationDataDir = GetApplicationDataDir();

    public static readonly string InstallDir = string.IsNullOrWhiteSpace(AppContext.BaseDirectory)
        ? throw new ArgumentException("Install directory shouldn't be empty or null")
        : AppContext.BaseDirectory;

    public static readonly string HttpCacheDir = Path.Combine(ApplicationDataDir, "HTTPAnime");
    public static string HttpCachePath(int animeId) => Path.Combine(HttpCacheDir, $"AnimeDoc_{animeId}.xml");
    public static readonly string MyListBackupDir = Path.Combine(ApplicationDataDir, "MyListBackup");
    public static readonly string MyListPath = Path.Combine(MyListBackupDir, "mylist.xml");
    public static readonly string OptionsPath = Path.Combine(ApplicationDataDir, "shizou-settings.json");
    public static readonly string SchemaPath = Path.Combine(ApplicationDataDir, "shizou-settings-schema.json");
    public static readonly string LogsDir = Path.Combine(ApplicationDataDir, "Logs");
    public static readonly string ImagesDir = Path.Combine(ApplicationDataDir, "Images");
    public static readonly string AnimePostersDir = Path.Combine(ImagesDir, "AnimePosters");
    public static readonly string AnimeTitlesPath = Path.Combine(ApplicationDataDir, "AnimeTitles.dat");
    public static readonly string ExtraFileDataDir = Path.Combine(ApplicationDataDir, "ExtraFileData");
    public static readonly string MyAnimeListTokenPath = Path.Combine(ApplicationDataDir, "MyAnimeListToken.json");
    public static readonly string IdentityDatabasePath = Path.Combine(ApplicationDataDir, "IdentityDB.sqlite3");
    public static readonly string CertificateDir = Path.Combine(ApplicationDataDir, "Certificate");
    public static readonly string AvDumpDir = Path.Combine(InstallDir, "AVDump3");
    public static readonly string CreatorImageDir = Path.Combine(ImagesDir, "Creators");
    public static readonly string DefaultCollectionViewDir = Path.Combine(ApplicationDataDir, "CollectionView");
    public static readonly string StaticFilesDir = Path.Combine(InstallDir, "StaticFiles");
    public static readonly string MissingAnimePosterPath = Path.Combine(StaticFilesDir, "missing_poster.png");

    public static string DatabasePath(string username) => Path.Combine(ApplicationDataDir, "ShizouDB" +
                                                                                           (string.IsNullOrWhiteSpace(username)
                                                                                               ? null
                                                                                               : $".{username.ToLowerInvariant()}") +
                                                                                           ".sqlite3");

    public static string AnimePosterPath(string imageFilename) => Path.Combine(AnimePostersDir, imageFilename);

    public static string CreatorImagePath(string imageFilename) => Path.Combine(CreatorImageDir, imageFilename);

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

    public static class ExtraFileData
    {
        public static string AttachmentHashMapPath => Path.Combine(ExtraFileDataDir, "AttachmentHashMap.json");
        public static string FileDir(string ed2K) => Path.Combine(ExtraFileDataDir, ed2K);
        public static string ThumbnailPath(string ed2K) => Path.Combine(FileDir(ed2K), "thumb.webp");
        public static string SubsDir(string ed2K) => Path.Combine(FileDir(ed2K), "Subtitles");
        public static string SubPath(string ed2K, int index) => Path.Combine(SubsDir(ed2K), $"{index}.ass");
        public static string AttachmentsDir(string ed2K) => Path.Combine(FileDir(ed2K), "Attachments");
        public static string AttachmentPath(string ed2K, string filename) => Path.Combine(AttachmentsDir(ed2K), filename);
    }
}
