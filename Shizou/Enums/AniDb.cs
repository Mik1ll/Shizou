﻿using System;
using System.Xml.Serialization;

namespace Shizou.Enums;

public enum AnimeType
{
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
    Other = 6
}

public enum EpisodeType
{
    [XmlEnum("1")]
    Episode = 1,
    [XmlEnum("2")]
    Credits = 2,
    [XmlEnum("3")]
    Special = 3,
    [XmlEnum("4")]
    Trailer = 4,
    [XmlEnum("5")]
    Parody = 5,
    [XmlEnum("6")]
    Other = 6
}

public static class EpisodeTypeExtensions
{
    public static (int number, EpisodeType type) ParseEpisode(this string str)
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
    Unknown = 0,
    Internal = 1,
    External = 2,
    Deleted = 3,
    Remote = 4
}

public enum MyListFileState
{
    Normal = 0,
    InvalidCrc = 1,
    SelfEdited = 2,
    SelfRipped = 10,
    Dvd = 11,
    Vhs = 12,
    Tv = 13,
    Theater = 14,
    Streamed = 15,
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
    Eyecancer
}