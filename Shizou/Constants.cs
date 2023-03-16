using System;
using System.IO;

namespace Shizou;

public class Constants
{
    public static readonly string ApplicationData = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Shizou");
    public static readonly string TempFilePath = Path.Combine(ApplicationData, "Temp");
    public static readonly string HttpCachePath = Path.Combine(ApplicationData, "HTTPAnime");
    public static readonly string MyListPath = Path.Combine(ApplicationData, "AniDbMyList.xml");
}
