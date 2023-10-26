﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using Shizou.Data;
using Shizou.Data.Enums;

namespace Shizou.Server.Options;

public class ShizouOptions
{
    public const string Shizou = "Shizou";

    public ImportOptions Import { get; set; } = new();

    public AniDbOptions AniDb { get; set; } = new();

    public MyAnimeListOptions MyAnimeList { get; set; } = new();


    public void SaveToFile()
    {
        Dictionary<string, object> json = new() { { Shizou, this } };
        var jsonSettings = JsonSerializer.Serialize(json,
            new JsonSerializerOptions { WriteIndented = true, IgnoreReadOnlyProperties = true, Converters = { new JsonStringEnumConverter() } });
        File.WriteAllText(FilePaths.OptionsPath, jsonSettings);
    }
}

public class ImportOptions
{
}

public class AniDbOptions
{
    public string Username { get; set; } = string.Empty;

    public string Password { get; set; } = string.Empty;

    public string ServerHost { get; set; } = "api.anidb.net";

    public string? ImageServerHost { get; set; }

    public ushort UdpServerPort { get; set; } = 9000;

    public ushort HttpServerPort { get; set; } = 9001;

    public ushort ClientPort { get; set; } = 4556;

    public MyListOptions MyList { get; set; } = new();

    public int FetchRelationDepth { get; set; } = 3;
}

public class MyListOptions
{
    public MyListState AbsentFileState { get; set; } = MyListState.Deleted;
    public MyListState PresentFileState { get; set; } = MyListState.Internal;
}

public class MyAnimeListOptions
{
    public string ClientId { get; set; } = string.Empty;
    public MyAnimeListToken? MyAnimeListToken { get; set; }
}

public record MyAnimeListToken(string AccessToken, DateTimeOffset AccessExpiration, string RefreshToken, DateTimeOffset RefreshExpiration);
