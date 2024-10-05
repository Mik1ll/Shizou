using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Json.More;
using Json.Schema;
using Json.Schema.Generation.Intents;
using Shizou.Data;
using Shizou.Data.Enums;
using JsonSchema = Json.Schema.Generation;

// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable PropertyCanBeMadeInitOnly.Global

namespace Shizou.Server.Options;

public class ShizouOptions : IValidatableObject
{
    public const string Shizou = "Shizou";

    private static readonly object FileLock = new();

    [JsonSchema.Description("Config related to the import/scanning process")]
    public ImportOptions Import { get; set; } = new();

    [JsonSchema.Description("Change AniDb config here")]
    public AniDbOptions AniDb { get; set; } = new();

    public MyAnimeListOptions MyAnimeList { get; set; } = new();

    [JsonSchema.Description("Generate a directory of symbolic links to files in a structure recognized by Jellyfin")]
    public CollectionViewOptions CollectionView { get; set; } = new();

    public static void GenerateSchema()
    {
        var innerBuilder =
            JsonSchema.JsonSchemaBuilderExtensions.FromType<ShizouOptions>(new JsonSchemaBuilder(),
                new JsonSchema.SchemaGeneratorConfiguration() { Optimize = false });
        var builder = new JsonSchemaBuilder()
            .Type(SchemaValueType.Object)
            .Properties((Shizou, innerBuilder.Build()));
        var schema = builder.Build();
        using var file = File.Create(FilePaths.SchemaPath);
        using var writer = new Utf8JsonWriter(file, new JsonWriterOptions() { Indented = true });
        schema.ToJsonDocument().WriteTo(writer);
    }


    public void SaveToFile()
    {
        lock (FileLock)
        {
            Dictionary<string, object> json = new() { { "$schema", Path.GetFileName(FilePaths.SchemaPath) }, { Shizou, this } };
            using var file = File.Create(FilePaths.OptionsPath);
            JsonSerializer.Serialize(file, json,
                new JsonSerializerOptions { WriteIndented = true, IgnoreReadOnlyProperties = true, Converters = { new JsonStringEnumConverter() } });
            file.Write(Encoding.ASCII.GetBytes(Environment.NewLine));
        }
    }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        var results = new List<ValidationResult>();

        Validator.TryValidateObject(AniDb, new ValidationContext(AniDb), results, true);

        return results;
    }
}

[AttributeUsage(AttributeTargets.Property)]
public class FormatAttribute : Attribute, JsonSchema.IAttributeHandler<FormatAttribute>
{
    public FormatAttribute(string format) => Format = format;

    public string Format { get; }

    public void AddConstraints(JsonSchema.SchemaGenerationContextBase context, Attribute attribute)
    {
        context.Intents.Add(new FormatIntent(Formats.Get(Format)));
    }
}

public class ImportOptions
{
    [JsonSchema.Description("The file extensions to scan, other files will be ignored")]
    public string[] FileExtensions { get; set; } =
    [
        // Video
        "mov", "mkv", "avi", "mp4", "wmv", "mpg", "mpeg", "m4v", "webm", "flv", "f4v", "ts", "m2ts", "mts", "vob", "ogm", "asf", "mk3d", "ogv", "qt", "rm",
        "rmvb", "swf",
        // Subtitle
        "srt", "ass", "ssa", "sub", "sbv", "vtt", "ttxt", "fsb", "idx", "js", "lrc", "mks", "pjs", "rt", "s2k", "smi", "sup", "tts", "txt", "xss", "zeg", "tmp",
        // Audio
        "ac3", "m4a", "mp3", "flac", "mka", "ogg", "aac", "dts", "dtshd", "mlp", "ra", "thd", "wav", "wma"
    ];

    [JsonSchema.Description("The path to ffmpeg, used for extracting episode thumbnails")]
    [JsonSchema.Nullable(true)]
    public string? FfmpegPath { get; set; }

    [JsonSchema.Description("The path to ffprobe, used for metadata extraction")]
    [JsonSchema.Nullable(true)]
    public string? FfprobePath { get; set; }
}

public class AniDbOptions
{
    private static readonly object UserLock = new();
    private static bool _userSet;
    private static string _username = string.Empty;

    [Required(ErrorMessage = "Shizou:AniDb:Username is required on startup", AllowEmptyStrings = false)]
    [StringLength(16, MinimumLength = 3, ErrorMessage = "Shizou:AniDb:Username must be between 3 and 16 characters")]
    [JsonSchema.Required]
    [JsonSchema.MinLength(3)]
    [JsonSchema.MaxLength(16)]
    [JsonSchema.Description("AniDb Username")]
    public string Username
    {
        get => _username;
        set
        {
            lock (UserLock)
            {
                if (!_userSet)
                    _username = value;
                _userSet = true;
            }
        }
    }

    [JsonSchema.MinLength(4)]
    [JsonSchema.MaxLength(64)]
    [JsonSchema.Pattern("^[a-zA-Z0-9]+$")]
    [JsonSchema.Description("Ensure AniDb password is not use anywhere else, it is insecure. Only use alphanumeric characters.")]
    public string Password { get; set; } = string.Empty;

    [Format("hostname")]
    [JsonSchema.Description("Host name of AniDb API (fully qualified domain name)")]
    public string ServerHost { get; set; } = "api.anidb.net";

    [Format("hostname")]
    [JsonSchema.Nullable(true)]
    [JsonSchema.Description("Host name of AniDb image server, this is autopopulated")]
    public string? ImageServerHost { get; set; }

    [JsonSchema.Description("Remote HTTP port used to connect to AniDb API, should be 9001")]
    public int HttpServerPort { get; set; } = 9001;

    [JsonSchema.Description("Remote UDP port used to connect to AniDb API, should be 9000")]
    public int UdpServerPort { get; set; } = 9000;

    [JsonSchema.Minimum(1024)]
    [JsonSchema.Maximum(65535)]
    [JsonSchema.Description("Local UDP port used to connect to API, NAT may affect the port that AniDb sees")]
    public int UdpClientPort { get; set; } = 4556;

    public MyListOptions MyList { get; set; } = new();

    [JsonSchema.Minimum(0)]
    [JsonSchema.Maximum(4)]
    [JsonSchema.Description("How many relation layers deep to fetch anime data, this may result in many HTTP requests if each anime has many relations")]
    public int FetchRelationDepth { get; set; } = 3;

    public AvDumpOptions AvDump { get; set; } = new();
}

public class MyListOptions
{
    [JsonSchema.Description("Disables Sync to AniDb command, use if concerned of AniDb data loss/clobbering")]
    public bool DisableSync { get; set; }

    [JsonSchema.Description("State to mark files if they are not present in local collection")]
    public MyListState AbsentFileState { get; set; } = MyListState.Deleted;

    [JsonSchema.Description("State to mark files if they are present in local collection")]
    public MyListState PresentFileState { get; set; } = MyListState.Internal;
}

public class MyAnimeListOptions
{
    public string ClientId { get; set; } = string.Empty;
}

public class AvDumpOptions
{
    [JsonSchema.Nullable(true)]
    [JsonSchema.MaxLength(32)]
    [JsonSchema.Pattern("^[a-zA-Z1-9]+$")]
    [JsonSchema.Description("AniDB UDP key, this is set in your account settings on AniDB")]
    public string? UdpKey { get; set; }

    [JsonSchema.Minimum(1024)]
    [JsonSchema.Maximum(65535)]
    [JsonSchema.Description("Local UDP port used by AVDump, NAT may affect the port that AniDb sees")]
    public int UdpClientPort { get; set; } = 4557;
}

public class CollectionViewOptions
{
    public bool Enabled { get; set; }

    [JsonSchema.Nullable(true)]
    [JsonSchema.Description("The full path of the target directory")]
    public string? Path { get; set; }
}
