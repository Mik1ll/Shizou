﻿using System.Xml.Serialization;

namespace Shizou.Data.Enums;

public enum AnimeType
{
    [XmlEnum("unknown")]
    Unknown = 0,

    [XmlEnum("Movie")]
    Movie = 1,

    [XmlEnum("OVA")]
    Ova = 2,

    [XmlEnum("TV Series")]
    TvSeries = 3,

    [XmlEnum("TV Special")]
    TvSpecial = 4,

    [XmlEnum("Web")]
    Web = 5,

    [XmlEnum("Other")]
    Other = 6,

    [XmlEnum("Music Video")]
    MusicVideo = 7
}

public enum EpisodeType
{
    [XmlEnum("1")]
    Episode = 1,

    [XmlEnum("2")]
    Special = 2,

    [XmlEnum("3")]
    Credits = 3,

    [XmlEnum("4")]
    Trailer = 4,

    [XmlEnum("5")]
    Parody = 5,

    [XmlEnum("6")]
    Other = 6
}

public static class EpisodeTypeExtensions
{
    public static string GetPrefix(this EpisodeType episodeType) =>
        episodeType switch
        {
            EpisodeType.Credits => "C",
            EpisodeType.Special => "S",
            EpisodeType.Trailer => "T",
            EpisodeType.Parody => "P",
            _ => ""
        };

    public static string GetEpString(this EpisodeType type, int number) => $"{type.GetPrefix()}{number}";

    public static (int number, EpisodeType type) ParseEpString(string str)
    {
        var num = int.Parse(char.IsNumber(str[0]) ? str : str[1..]);
        var type = str[0] switch
        {
            'C' => EpisodeType.Credits,
            'S' => EpisodeType.Special,
            'T' => EpisodeType.Trailer,
            'P' => EpisodeType.Parody,
            _ => EpisodeType.Episode
        };
        return (num, type);
    }
}

[Flags]
public enum FileState
{
    CrcOk = 1,
    CrcError = 1 << 1,
    Ver2 = 1 << 2,
    Ver3 = 1 << 3,
    Ver4 = 1 << 4,
    Ver5 = 1 << 5,
    Uncensored = 1 << 6,
    Censored = 1 << 7,
    Chaptered = 1 << 12
}

public static class FileStateExtensions
{
    public static bool? IsCensored(this FileState state)
    {
        return state.HasFlag(FileState.Censored) ? true : state.HasFlag(FileState.Uncensored) ? false : null;
    }

    public static int FileVersion(this FileState state)
    {
        return state.HasFlag(FileState.Ver2) ? 2 :
            state.HasFlag(FileState.Ver3) ? 3 :
            state.HasFlag(FileState.Ver4) ? 4 :
            state.HasFlag(FileState.Ver5) ? 5 : 1;
    }
}

public enum MyListState
{
    [XmlEnum("0")]
    Unknown = 0,

    [XmlEnum("1")]
    Internal = 1,

    [XmlEnum("2")]
    External = 2,

    [XmlEnum("3")]
    Deleted = 3,

    [XmlEnum("4")]
    Remote = 4
}

public enum MyListFileState
{
    [XmlEnum("0")]
    Normal = 0,

    [XmlEnum("1")]
    InvalidCrc = 1,

    [XmlEnum("2")]
    SelfEdited = 2,

    [XmlEnum("10")]
    SelfRipped = 10,

    [XmlEnum("11")]
    Dvd = 11,

    [XmlEnum("12")]
    Vhs = 12,

    [XmlEnum("13")]
    Tv = 13,

    [XmlEnum("14")]
    Theater = 14,

    [XmlEnum("15")]
    Streamed = 15,

    /// <summary>
    ///     Doesn't work in mylist add
    /// </summary>
    [XmlEnum("16")]
    BluRay = 16,

    /// <summary>
    ///     Doesn't work in mylist add
    /// </summary>
    [XmlEnum("17")]
    Www = 17,

    [XmlEnum("100")]
    Other = 100
}

public enum RelatedAnimeType
{
    Sequel = 1,
    Prequel = 2,
    SameSetting = 11,
    AlternativeSetting = 12,
    AlternativeVersion = 32,
    MusicVideo = 41,
    Character = 42,
    SideStory = 51,
    ParentStory = 52,
    Summary = 61,
    FullStory = 62,
    Other = 100
}

public enum FileSource
{
    Unknown,
    Camcorder,
    Tv,
    Dtv,
    Hdtv,
    Vhs,
    Vcd,
    Svcd,
    Ld,
    Dvd,
    HkDvd,
    HdDvd,
    Bluray,
    Www
}

public enum FileQuality
{
    Unknown,
    VeryHigh,
    High,
    Med,
    Low,
    VeryLow,
    Corrupted,
    EyeCancer
}

public enum NotificationType
{
    All = 0,
    New = 1,
    Group = 2,
    Complete = 3
}

public enum MessageType
{
    Normal = 0,
    Anonymous = 1,
    System = 2,
    Mod = 3
}

public enum ResourceType
{
    Ann = 1,
    Mal = 2,
    AnimeNfo = 3,
    OfficialJapanese = 4,
    OfficialEnglish = 5,
    WikiEnglish = 6,
    WikiJapanese = 7,
    Syoboi = 8,
    AllCinema = 9,
    Anison = 10,
    DotLain = 11,
    Vndb = 14,
    Crunchyroll = 28,
    Amazon = 32,
    Funimation = 34,
    Bangumi = 38,
    HiDive = 42
}

public enum CreatorType
{
    Unknown = 0,
    Person = 1,
    Company = 2,
    Collaboration = 3,
    Other = 4
}

public enum CharacterType
{
    Unknown = 0,
    Character = 1,
    Mecha = 2,
    Organization = 3,
    Vehicle = 4
}
