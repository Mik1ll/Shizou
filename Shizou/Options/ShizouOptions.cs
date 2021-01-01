using System;
using System.IO;
using System.Net;
using System.Text.Json;

namespace Shizou.Options
{
    public class ShizouOptions
    {
        public static readonly string OptionsPath = Path.Combine(Program.ApplicationData, "shizou-settings.json");

        public static void SaveSettingsToFile(ShizouOptions options)
        {
            var jsonSettings = $"{{{Environment.NewLine}  \"{Shizou}\": {JsonSerializer.Serialize(options, new JsonSerializerOptions { WriteIndented = true, IgnoreReadOnlyProperties = true }).Replace(Environment.NewLine, $"{Environment.NewLine}  ")}{Environment.NewLine}}}";
            File.WriteAllText(OptionsPath, jsonSettings);
        }

        public const string Shizou = "Shizou";

        public ImportOptions Import { get; set; } = new ImportOptions();

        public AniDBOptions AniDB { get; set; } = new AniDBOptions();

        public class ImportOptions
        {
        }

        public class AniDBOptions
        {
            public string Username { get; set; } = string.Empty;

            public string Password { get; set; } = string.Empty;

            public string ServerHost { get; set; } = "api.anidb.net";

            public ushort ServerPort { get; set; } = 9000;
        }
    }
}