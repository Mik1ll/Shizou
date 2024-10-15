using System;
using Shizou.Data.Enums;

namespace Shizou.Server.AniDbApi.Requests.Udp;

public record CreatorResult
{
    public CreatorResult(string responseText)
    {
        var dataArr = responseText.Split('|');
        CreatorId = int.Parse(dataArr[0]);
        CreatorNameKanji = string.IsNullOrWhiteSpace(dataArr[1]) ? null : dataArr[1];
        CreatorNameTranscription = string.IsNullOrWhiteSpace(dataArr[2]) ? null : dataArr[2];
        CreatorType = Enum.Parse<CreatorType>(dataArr[3]);
        PictureFilename = string.IsNullOrWhiteSpace(dataArr[4]) ? null : dataArr[4];
        UrlEnglish = string.IsNullOrWhiteSpace(dataArr[5]) ? null : dataArr[5];
        UrlJapanese = string.IsNullOrWhiteSpace(dataArr[6]) ? null : dataArr[6];
        WikiUrlEnglish = string.IsNullOrWhiteSpace(dataArr[7]) ? null : dataArr[7];
        WikiUrlJapanese = string.IsNullOrWhiteSpace(dataArr[8]) ? null : dataArr[8];
        Updated = DateTimeOffset.FromUnixTimeSeconds(long.Parse(dataArr[9]));
    }

    public int CreatorId { get; }
    public string? CreatorNameKanji { get; }
    public string? CreatorNameTranscription { get; }
    public CreatorType CreatorType { get; }
    public string? PictureFilename { get; }
    public string? UrlEnglish { get; }
    public string? UrlJapanese { get; }
    public string? WikiUrlEnglish { get; }
    public string? WikiUrlJapanese { get; }
    public DateTimeOffset Updated { get; }
}
