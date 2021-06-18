using System;

namespace Shizou.Enums
{
    public enum AnimeType
    {
        None = -1, // Not on AniDB, but for ease of processing
        Movie = 0,
        Ova = 1,
        TvSeries = 2,
        TvSpecial = 3,
        Web = 4,
        Other = 5
    }

    public enum EpisodeType
    {
        Episode = 1,
        Credits = 2,
        Special = 3,
        Trailer = 4,
        Parody = 5,
        Other = 6
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
}
