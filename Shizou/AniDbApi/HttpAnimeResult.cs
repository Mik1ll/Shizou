using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Serialization;
using Shizou.Entities;
using Shizou.Enums;

namespace Shizou.AniDbApi
{
    [XmlRoot(ElementName = "anime")]
    public class HttpAnimeResult
    {
        [XmlAttribute(AttributeName = "id")]
        public int Id { get; set; }

        [XmlElement(ElementName = "type")]
        public AnimeType Type { get; set; }

        [XmlElement(ElementName = "episodecount")]
        public int Episodecount { get; set; }

        [XmlElement(ElementName = "startdate")]
        public DateTime Startdate { get; set; }

        [XmlElement(ElementName = "enddate")]
        public DateTime Enddate { get; set; }

        [XmlElement(ElementName = "titles")]
        public AnimeTitles Titles { get; set; }

        [XmlElement(ElementName = "relatedanime")]
        public Relatedanime Relatedanime { get; set; }

        [XmlElement(ElementName = "similaranime")]
        public Similaranime Similaranime { get; set; }

        [XmlElement(ElementName = "recommendations")]
        public Recommendations Recommendations { get; set; }

        [XmlElement(ElementName = "url")]
        public string Url { get; set; }

        [XmlElement(ElementName = "creators")]
        public Creators Creators { get; set; }

        [XmlElement(ElementName = "description")]
        public string Description { get; set; }

        [XmlElement(ElementName = "ratings")]
        public Ratings Ratings { get; set; }

        [XmlElement(ElementName = "picture")]
        public string Picture { get; set; }

        [XmlElement(ElementName = "resources")]
        public Resources Resources { get; set; }

        [XmlElement(ElementName = "tags")]
        public Tags Tags { get; set; }

        [XmlElement(ElementName = "characters")]
        public Characters Characters { get; set; }

        [XmlElement(ElementName = "episodes")]
        public Episodes Episodes { get; set; }

        [XmlAttribute(AttributeName = "restricted")]
        public bool Restricted { get; set; }

        public AniDbAnime ToAniDbAnime()
        {
            var mainTitle = Titles.Title.First(t => t.Type == "main");
            var anime = new AniDbAnime
            {
                Id = Id,
                Description = Description,
                Restricted = Restricted,
                AirDate = Startdate,
                EndDate = Enddate,
                AnimeType = Type,
                EpisodeCount = Episodecount,
                ImagePath = Picture,
                Title = mainTitle.Text,
                AniDbEpisodes = Episodes.Episode.Select(e => new AniDbEpisode
                {
                    Id = e.Id,
                    Duration = TimeSpan.FromMinutes(e.Length),
                    Number = e.Epno.Text,
                    EpisodeType = e.Epno.Type,
                    AirDate = e.Airdate,
                    Updated = DateTime.UtcNow,
                    AniDbAnimeId = Id,
                    TitleEnglish = e.Title.First(t => t.Lang == "en").Text,
                    TitleRomaji = e.Title.FirstOrDefault(t => t.Lang.StartsWith("x-") && t.Lang == mainTitle.Lang)?.Text,
                    TitleKanji = e.Title.FirstOrDefault(t =>
                        t.Lang.StartsWith(mainTitle.Lang switch {"x-jat" => "ja", "x-zht" => "zh-han", "x-kot" => "ko", _ => "none"},
                            StringComparison.OrdinalIgnoreCase))?.Text
                }).ToList()
            };
            return anime;
        }
    }

    [XmlRoot(ElementName = "title")]
    public class AnimeTitle
    {
        [XmlAttribute(AttributeName = "lang")]
        public string Lang { get; set; }

        [XmlAttribute(AttributeName = "type")]
        public string Type { get; set; }

        [XmlText]
        public string Text { get; set; }
    }

    [XmlRoot(ElementName = "title")]
    public class EpisodeTitle
    {
        [XmlAttribute(AttributeName = "lang")]
        public string Lang { get; set; }

        [XmlText]
        public string Text { get; set; }
    }

    [XmlRoot(ElementName = "titles")]
    public class AnimeTitles
    {
        [XmlElement(ElementName = "title")]
        public List<AnimeTitle> Title { get; set; }
    }

    [XmlRoot(ElementName = "anime")]
    public class RelatedAnimeEntry
    {
        [XmlAttribute(AttributeName = "id")]
        public int Id { get; set; }

        [XmlAttribute(AttributeName = "type")]
        public string Type { get; set; }

        [XmlText]
        public string Text { get; set; }
    }

    [XmlRoot(ElementName = "anime")]
    public class SimilarAnimeEntry
    {
        [XmlAttribute(AttributeName = "id")]
        public int Id { get; set; }

        [XmlAttribute(AttributeName = "approval")]
        public int Approval { get; set; }

        [XmlAttribute(AttributeName = "total")]
        public int Total { get; set; }

        [XmlText]
        public string Text { get; set; }
    }

    [XmlRoot(ElementName = "relatedanime")]
    public class Relatedanime
    {
        [XmlElement(ElementName = "anime")]
        public List<RelatedAnimeEntry> Anime { get; set; }
    }

    [XmlRoot(ElementName = "similaranime")]
    public class Similaranime
    {
        [XmlElement(ElementName = "anime")]
        public List<SimilarAnimeEntry> Anime { get; set; }
    }

    [XmlRoot(ElementName = "recommendation")]
    public class Recommendation
    {
        [XmlAttribute(AttributeName = "type")]
        public string Type { get; set; }

        [XmlAttribute(AttributeName = "uid")]
        public int Uid { get; set; }

        [XmlText]
        public string Text { get; set; }
    }

    [XmlRoot(ElementName = "recommendations")]
    public class Recommendations
    {
        [XmlElement(ElementName = "recommendation")]
        public List<Recommendation> Recommendation { get; set; }

        [XmlAttribute(AttributeName = "total")]
        public int Total { get; set; }

        [XmlText]
        public string Text { get; set; }
    }

    [XmlRoot(ElementName = "name")]
    public class CreatorName
    {
        [XmlAttribute(AttributeName = "id")]
        public int Id { get; set; }

        [XmlAttribute(AttributeName = "type")]
        public string Type { get; set; }

        [XmlText]
        public string Text { get; set; }
    }

    [XmlRoot(ElementName = "creators")]
    public class Creators
    {
        [XmlElement(ElementName = "name")]
        public List<CreatorName> Name { get; set; }
    }

    [XmlRoot(ElementName = "ratings")]
    public class Ratings
    {
        [XmlElement(ElementName = "permanent")]
        public Rating Permanent { get; set; }

        [XmlElement(ElementName = "temporary")]
        public Rating Temporary { get; set; }

        [XmlElement(ElementName = "review")]
        public Rating Review { get; set; }
    }

    [XmlRoot(ElementName = "externalentity")]
    public class Externalentity
    {
        [XmlElement(ElementName = "identifier")]
        public List<string> Identifier { get; set; }

        [XmlElement(ElementName = "url")]
        public string Url { get; set; }
    }

    [XmlRoot(ElementName = "resource")]
    public class Resource
    {
        [XmlElement(ElementName = "externalentity")]
        public Externalentity Externalentity { get; set; }

        [XmlAttribute(AttributeName = "type")]
        public int Type { get; set; }

        [XmlText]
        public int Text { get; set; }
    }

    [XmlRoot(ElementName = "resources")]
    public class Resources
    {
        [XmlElement(ElementName = "resource")]
        public List<Resource> Resource { get; set; }
    }

    [XmlRoot(ElementName = "tag")]
    public class Tag
    {
        [XmlElement(ElementName = "name")]
        public string Name { get; set; }

        [XmlElement(ElementName = "description")]
        public string Description { get; set; }

        [XmlElement(ElementName = "picurl")]
        public string Picurl { get; set; }

        [XmlAttribute(AttributeName = "id")]
        public int Id { get; set; }

        [XmlAttribute(AttributeName = "parentid")]
        public int Parentid { get; set; }

        [XmlAttribute(AttributeName = "weight")]
        public int Weight { get; set; }

        [XmlAttribute(AttributeName = "localspoiler")]
        public bool Localspoiler { get; set; }

        [XmlAttribute(AttributeName = "globalspoiler")]
        public bool Globalspoiler { get; set; }

        [XmlAttribute(AttributeName = "verified")]
        public bool Verified { get; set; }

        [XmlAttribute(AttributeName = "update")]
        public DateTime Update { get; set; }

        [XmlText]
        public string Text { get; set; }

        [XmlAttribute(AttributeName = "infobox")]
        public bool Infobox { get; set; }
    }

    [XmlRoot(ElementName = "tags")]
    public class Tags
    {
        [XmlElement(ElementName = "tag")]
        public List<Tag> Tag { get; set; }
    }

    [XmlRoot(ElementName = "rating")]
    public class Rating
    {
        [XmlAttribute(AttributeName = "votes")]
        public int Votes { get; set; }

        [XmlText]
        public DateTime Text { get; set; }
    }

    [XmlRoot(ElementName = "charactertype")]
    public class Charactertype
    {
        [XmlAttribute(AttributeName = "id")]
        public int Id { get; set; }

        [XmlText]
        public string Text { get; set; }
    }

    [XmlRoot(ElementName = "seiyuu")]
    public class Seiyuu
    {
        [XmlAttribute(AttributeName = "id")]
        public int Id { get; set; }

        [XmlAttribute(AttributeName = "picture")]
        public string Picture { get; set; }

        [XmlText]
        public string Text { get; set; }
    }

    [XmlRoot(ElementName = "character")]
    public class Character
    {
        [XmlElement(ElementName = "rating")]
        public Rating Rating { get; set; }

        [XmlElement(ElementName = "name")]
        public string Name { get; set; }

        [XmlElement(ElementName = "gender")]
        public string Gender { get; set; }

        [XmlElement(ElementName = "charactertype")]
        public Charactertype Charactertype { get; set; }

        [XmlElement(ElementName = "description")]
        public string Description { get; set; }

        [XmlElement(ElementName = "picture")]
        public string Picture { get; set; }

        [XmlElement(ElementName = "seiyuu")]
        public Seiyuu Seiyuu { get; set; }

        [XmlAttribute(AttributeName = "id")]
        public int Id { get; set; }

        [XmlAttribute(AttributeName = "type")]
        public string Type { get; set; }

        [XmlAttribute(AttributeName = "update")]
        public DateTime Update { get; set; }
    }

    [XmlRoot(ElementName = "characters")]
    public class Characters
    {
        [XmlElement(ElementName = "character")]
        public List<Character> Character { get; set; }
    }

    [XmlRoot(ElementName = "epno")]
    public class Epno
    {
        [XmlAttribute(AttributeName = "type")]
        public EpisodeType Type { get; set; }

        [XmlText]
        public int Text { get; set; }
    }

    [XmlRoot(ElementName = "episode")]
    public class Episode
    {
        [XmlElement(ElementName = "epno")]
        public Epno Epno { get; set; }

        [XmlElement(ElementName = "length")]
        public int Length { get; set; }

        [XmlElement(ElementName = "airdate")]
        public DateTime Airdate { get; set; }

        [XmlElement(ElementName = "rating")]
        public Rating Rating { get; set; }

        [XmlElement(ElementName = "title")]
        public List<EpisodeTitle> Title { get; set; }

        [XmlAttribute(AttributeName = "id")]
        public int Id { get; set; }

        [XmlAttribute(AttributeName = "update")]
        public DateTime Update { get; set; }
    }

    [XmlRoot(ElementName = "episodes")]
    public class Episodes
    {
        [XmlElement(ElementName = "episode")]
        public List<Episode> Episode { get; set; }
    }
}
