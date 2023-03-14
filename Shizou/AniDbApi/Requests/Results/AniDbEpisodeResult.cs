using System;
using Shizou.Enums;

namespace Shizou.AniDbApi.Requests.Results;

public sealed record AniDbEpisodeResult
{
    public AniDbEpisodeResult(string responseText)
    {
        var dataArr = responseText.Split('|');
        EpisodeId = int.Parse(dataArr[0]);
        AnimeId = int.Parse(dataArr[1]);
        DurationMinutes = dataArr[1] != "0" ? int.Parse(dataArr[2]) : null;
        Rating = int.Parse(dataArr[3]);
        Votes = int.Parse(dataArr[4]);
        (EpisodeNumber, Type) = dataArr[5].ParseEpisode();
        TitleEnglish = dataArr[6];
        TitleRomaji = dataArr[7];
        TitleKanji = dataArr[8];
        AiredDate = dataArr[9] != "0" ? DateTimeOffset.FromUnixTimeSeconds(long.Parse(dataArr[9])).UtcDateTime : null;
    }

    public int EpisodeId { get; }
    public int AnimeId { get; }
    public int? DurationMinutes { get; }
    public int Rating { get; }
    public int Votes { get; }
    public int EpisodeNumber { get; }
    public EpisodeType Type { get; }
    public string TitleEnglish { get; }
    public string TitleRomaji { get; }
    public string TitleKanji { get; }
    public DateTime? AiredDate { get; }
}
