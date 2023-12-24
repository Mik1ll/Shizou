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

    public const string Schema = /*lang=json*/ """
                                               {
                                                 "$schema": "https://json-schema.org/draft-07/schema",
                                                 "type": "object",
                                                 "properties": {
                                                   "Shizou": {
                                                     "type": "object",
                                                     "properties": {
                                                       "Import": {
                                                         "type": "object"
                                                       },
                                                       "AniDb": {
                                                         "type": "object",
                                                         "description": "Change AniDb config here",
                                                         "properties": {
                                                           "Username": {
                                                             "description": "AniDb Username",
                                                             "type": "string"
                                                           },
                                                           "Password": {
                                                             "description": "Ensure AniDb password is not use anywhere else, it is insecure. Only use alphanumeric characters.",
                                                             "type": "string",
                                                             "pattern": "^[a-zA-z0-9]+$",
                                                             "minLength": 4,
                                                             "maxLength": 64
                                                           },
                                                           "ServerHost": {
                                                             "description": "Host name of AniDb API (fully qualified domain name)",
                                                             "type": "string",
                                                             "format": "hostname"
                                                           },
                                                           "ImageServerHost": {
                                                             "description": "Host name of AniDb image server, this is autopopulated",
                                                             "type": [
                                                               "string",
                                                               "null"
                                                             ],
                                                             "format": "hostname"
                                                           },
                                                           "UdpServerPort": {
                                                             "description": "Remote port used to connect to AniDb UDP API, should be 9000",
                                                             "type": "integer"
                                                           },
                                                           "HttpServerPort": {
                                                             "description": "Remote port used to connect to AniDb HTTP API, should be 9001",
                                                             "type": "integer"
                                                           },
                                                           "ClientPort": {
                                                             "description": "Local port used to connect to UDP API, NAT may affect the port that AniDb sees",
                                                             "type": "integer",
                                                             "minimum": 1024,
                                                             "maximum": 65535
                                                           },
                                                           "MyList": {
                                                             "type": "object",
                                                             "properties": {
                                                               "DisableSync": {
                                                                 "description": "Disables Sync to AniDb command, use if concerned of AniDb data loss/clobbering",
                                                                 "type": "boolean"
                                                               },
                                                               "AbsentFileState": {
                                                                 "description": "State to mark files if they are not present in local collection",
                                                                 "enum": [
                                                                   "Unknown",
                                                                   "Internal",
                                                                   "External",
                                                                   "Deleted",
                                                                   "Remote"
                                                                 ]
                                                               },
                                                               "PresentFileState": {
                                                                 "description": "State to mark files if they are present in local collection",
                                                                 "enum": [
                                                                   "Unknown",
                                                                   "Internal",
                                                                   "External",
                                                                   "Deleted",
                                                                   "Remote"
                                                                 ]
                                                               }
                                                             }
                                                           },
                                                           "FetchRelationDepth": {
                                                             "description": "How many relation layers deep to fetch anime data, this may result in many HTTP requests if each anime has many relations",
                                                             "type": "integer",
                                                             "minimum": 0,
                                                             "maximum": 4
                                                           }
                                                         }
                                                       },
                                                       "MyAnimeList": {
                                                         "type": "object",
                                                         "properties": {
                                                           "ClientId": {
                                                             "type": "string"
                                                           },
                                                           "MyAnimeListToken": {
                                                             "type": [
                                                               "object",
                                                               "null"
                                                             ],
                                                             "properties": {
                                                               "AccessToken": {
                                                                 "type": "string"
                                                               },
                                                               "AccessExpiration": {
                                                                 "type": "string",
                                                                 "format": "date-time"
                                                               },
                                                               "RefreshToken": {
                                                                 "type": "string"
                                                               },
                                                               "RefreshExpiration": {
                                                                 "type": "string",
                                                                 "format": "date-time"
                                                               }
                                                             }
                                                           }
                                                         }
                                                       }
                                                     }
                                                   }
                                                 }
                                               }
                                               """;

    public ImportOptions Import { get; set; } = new();

    public AniDbOptions AniDb { get; set; } = new();

    public MyAnimeListOptions MyAnimeList { get; set; } = new();


    public void SaveToFile()
    {
        Dictionary<string, object> json = new() { { "$schema", Path.GetFileName(FilePaths.SchemaPath) }, { Shizou, this } };
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
    public bool DisableSync { get; set; } = false;
    public MyListState AbsentFileState { get; set; } = MyListState.Deleted;
    public MyListState PresentFileState { get; set; } = MyListState.Internal;
}

public class MyAnimeListOptions
{
    public string ClientId { get; set; } = string.Empty;
}
