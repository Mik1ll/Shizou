using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using Shizou.AniDbApi.Results;
using Shizou.AniDbApi.Results.HttpAnimeSubTypes;
using Shizou.Enums;

namespace Shizou.Models;

public class AniDbEpisode : IEntity
{
    public AniDbEpisode()
    {
    }

    public AniDbEpisode(AniDbEpisodeResult result)
    {
        Id = result.EpisodeId;
        TitleEnglish = result.TitleEnglish;
        TitleRomaji = result.TitleRomaji;
        TitleKanji = result.TitleKanji;
        Number = result.EpisodeNumber;
        EpisodeType = result.Type;
        DurationMinutes = result.DurationMinutes is null ? null : result.DurationMinutes.Value;
        AirDate = result.AiredDate;
        Updated = DateTime.UtcNow;
        AniDbAnimeId = result.AnimeId;
    }

    public AniDbEpisode(AniDbFileResult result)
    {
        Id = result.EpisodeId!.Value;
        TitleEnglish = result.EpisodeTitleEnglish!;
        TitleRomaji = result.EpisodeTitleRomaji;
        TitleKanji = result.EpisodeTitleKanji;
        AirDate = result.EpisodeAiredDate;
    }

    public AniDbEpisode(Episode episode, string animeLang)
    {
        Id = episode.Id;
        DurationMinutes = episode.Length;
        Number = episode.Epno.Text.ParseEpisode().number;
        EpisodeType = episode.Epno.Type;
        AirDate = episode.Airdate;
        Updated = DateTime.UtcNow;
        AniDbAnimeId = Id;
        TitleEnglish = episode.Title.First(t => t.Lang == "en").Text;
        TitleRomaji = episode.Title.FirstOrDefault(t => t.Lang.StartsWith("x-") && t.Lang == animeLang)?.Text;
        TitleKanji = episode.Title.FirstOrDefault(t =>
            t.Lang.StartsWith(animeLang switch { "x-jat" => "ja", "x-zht" => "zh-han", "x-kot" => "ko", _ => "none" },
                StringComparison.OrdinalIgnoreCase))?.Text;
    }

    [DatabaseGenerated(DatabaseGeneratedOption.None)]
    public int Id { get; set; }

    public string TitleEnglish { get; set; } = null!;
    public string? TitleRomaji { get; set; }
    public string? TitleKanji { get; set; }
    public int Number { get; set; }
    public EpisodeType EpisodeType { get; set; }
    public int? DurationMinutes { get; set; }
    public DateTime? AirDate { get; set; }
    public DateTime? Updated { get; set; }


    public int AniDbAnimeId { get; set; }
    public AniDbAnime AniDbAnime { get; set; } = null!;

    public int? GenericMyListEntryId { get; set; }
    public AniDbMyListEntry? GenericMyListEntry { get; set; }

    public List<LocalFile> ManualLinkLocalFiles { get; set; } = null!;
}
