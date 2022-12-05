using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using Shizou.AniDbApi.Results;
using Shizou.Enums;

namespace Shizou.Models;

public sealed class AniDbAnime : IEntity
{
    public AniDbAnime()
    {
    }

    public AniDbAnime(AniDbAnimeResult result)
    {
        Id = result.AnimeId!.Value;
        Title = result.TitleRomaji!;
        EpisodeCount = result.TotalEpisodes!.Value;
        HighestEpisode = result.HighestEpisodeNumber!.Value;
        AnimeType = result.Type!.Value;
        AniDbUpdated = result.DateRecordUpdated!.Value;
    }

    public AniDbAnime(AniDbFileResult result)
    {
        Id = result.AnimeId!.Value;
        Title = result.TitleRomaji!;
        EpisodeCount = result.TotalEpisodes!.Value;
        HighestEpisode = result.HighestEpisodeNumber!.Value;
        AnimeType = result.Type!.Value;
        AniDbUpdated = result.DateRecordUpdated!.Value;
    }

    public AniDbAnime(HttpAnimeResult result)
    {
        var mainTitle = result.Titles.First(t => t.Type == "main");
        Id = result.Id;
        Description = result.Description;
        Restricted = result.Restricted;
        AirDate = result.Startdate;
        EndDate = result.Enddate;
        AnimeType = result.Type;
        EpisodeCount = result.Episodecount;
        ImagePath = result.Picture;
        Title = mainTitle.Text;
        AniDbEpisodes = result.Episodes.Select(e => new AniDbEpisode(e, mainTitle.Lang)).ToList();
        Updated = DateTime.UtcNow;
    }

    [DatabaseGenerated(DatabaseGeneratedOption.None)]
    public int Id { get; set; }

    public string Title { get; set; } = null!;
    public AnimeType AnimeType { get; set; }
    public int EpisodeCount { get; set; }
    public int HighestEpisode { get; set; }
    public string? AirDate { get; set; }
    public string? EndDate { get; set; }
    public string? Description { get; set; }
    public bool Restricted { get; set; }
    public string? ImagePath { get; set; }
    public DateTime? Updated { get; set; }
    public DateTime AniDbUpdated { get; set; }

    public List<AniDbEpisode> AniDbEpisodes { get; set; } = null!;
}