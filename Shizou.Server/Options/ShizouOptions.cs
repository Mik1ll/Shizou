using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using Shizou.Data;
using Shizou.Data.Enums;

namespace Shizou.Server.Options;

public class ShizouOptions
{
    public const string Shizou = "Shizou";

    public ImportOptions Import { get; set; } = new();

    public AniDbOptions AniDb { get; set; } = new();

    public MyListOptions MyList { get; set; } = new();

    public void SaveToFile()
    {
        Dictionary<string, object> json = new() { { Shizou, this } };
        var jsonSettings = JsonSerializer.Serialize(json, new JsonSerializerOptions { WriteIndented = true, IgnoreReadOnlyProperties = true });
        File.WriteAllText(FilePaths.OptionsPath, jsonSettings);
    }

    public class ImportOptions
    {
    }

    public class AniDbOptions
    {
        public string Username { get; set; } = string.Empty;

        public string Password { get; set; } = string.Empty;

        public string ServerHost { get; set; } = "api.anidb.net";

        public ushort UdpServerPort { get; set; } = 9000;

        public ushort HttpServerPort { get; set; } = 9001;

        public ushort ClientPort { get; set; } = 4556;
    }

    public class MyListOptions
    {
        public MyListState AbsentFileState = MyListState.Deleted;
        public MyListState PresentFileState = MyListState.Internal;
    }
}