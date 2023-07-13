using System;
using Shizou.Data.Enums;

namespace Shizou.Server.AniDbApi.Requests.Udp;

public sealed record EpisodeResult
{
    public EpisodeResult(string responseText)
    {
        var dataArr = responseText.Split('|');
        EpisodeId = int.Parse(dataArr[0]);
        AnimeId = int.Parse(dataArr[1]);
        DurationMinutes = dataArr[1] != "0" ? int.Parse(dataArr[2]) : null;
        Rating = int.Parse(dataArr[3]);
        Votes = int.Parse(dataArr[4]);
        (EpisodeNumber, Type) = EpisodeTypeExtensions.ParseEpisode(dataArr[5]);
        TitleEnglish = dataArr[6];
        TitleRomaji = dataArr[7];
        TitleKanji = dataArr[8];
        AiredDate = dataArr[9] != "0" ? DateTimeOffset.FromUnixTimeSeconds(long.Parse(dataArr[9])) : null;
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
    public DateTimeOffset? AiredDate { get; }
}
