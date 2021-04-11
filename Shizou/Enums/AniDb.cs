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
}
