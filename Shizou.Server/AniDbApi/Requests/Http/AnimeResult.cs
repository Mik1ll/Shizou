using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Xml.Serialization;
using Shizou.Data.Enums;
using Shizou.Server.AniDbApi.Requests.Http.SubElements;

// ReSharper disable RedundantNullableFlowAttribute

#pragma warning disable 8618

namespace Shizou.Server.AniDbApi.Requests.Http
{
    [XmlRoot("anime")]
    public class AnimeResult
    {
        [XmlAttribute("id")]
        public int Id { get; set; }

        [XmlElement("type")]
        public AnimeType Type { get; set; }

        [XmlElement("episodecount")]
        public int Episodecount { get; set; }

        [XmlElement("startdate")]
        public string? Startdate { get; set; }

        [XmlElement("enddate")]
        public string? Enddate { get; set; }

        [XmlArray("titles")]
        [XmlArrayItem("title")]
        public List<AnimeTitle> Titles { get; set; }

        [XmlArray("relatedanime")]
        [XmlArrayItem("anime")]
        public List<RelatedAnime> Relatedanime { get; set; }

        [XmlArray("similaranime")]
        [XmlArrayItem("anime")]
        public List<SimilarAnime> Similaranime { get; set; }

        [XmlElement("recommendations")]
        public Recommendations? Recommendations { get; set; }

        [XmlElement("url")]
        public string? Url { get; set; }

        [XmlArray("creators")]
        [XmlArrayItem("name")]
        public List<CreatorName> Creators { get; set; }

        [XmlElement("description")]
        public string? Description { get; set; }

        [XmlElement("ratings")]
        public Ratings? Ratings { get; set; }

        [XmlElement("picture")]
        public string? Picture { get; set; }

        [XmlArray("resources")]
        [XmlArrayItem("resource")]
        public List<Resource> Resources { get; set; }

        [XmlArray("tags")]
        [XmlArrayItem("tag")]
        public List<Tag> Tags { get; set; }

        [XmlArray("characters")]
        [XmlArrayItem("character")]
        public List<Character> Characters { get; set; }

        [XmlArray("episodes")]
        [XmlArrayItem("episode")]
        public List<Episode> Episodes { get; set; }

        [XmlAttribute("restricted")]
        public bool Restricted { get; set; }
    }

    namespace SubElements
    {
        [XmlRoot("title")]
        public class AnimeTitle
        {
            [XmlAttribute("xml:lang", DataType = "language")]
            public string Lang { get; set; }

            [XmlAttribute("type")]
            public string Type { get; set; }

            [XmlText]
            public string Text { get; set; }
        }

        [XmlRoot("title")]
        public class EpisodeTitle
        {
            [XmlAttribute("xml:lang", DataType = "language")]
            public string Lang { get; set; }

            [XmlText]
            public string Text { get; set; }
        }

        [XmlRoot("anime")]
        public class RelatedAnime
        {
            [XmlAttribute("id")]
            public int Id { get; set; }

            [XmlAttribute("type")]
            public string Type { get; set; }

            [XmlText]
            public string Text { get; set; }
        }

        [XmlRoot("anime")]
        public class SimilarAnime
        {
            [XmlAttribute("id")]
            public int Id { get; set; }

            [XmlAttribute("approval")]
            public int Approval { get; set; }

            [XmlAttribute("total")]
            public int Total { get; set; }

            [XmlText]
            public string Text { get; set; }
        }

        [XmlRoot("recommendation")]
        public class Recommendation
        {
            [XmlAttribute("type")]
            public string Type { get; set; }

            [XmlAttribute("uid")]
            public int Uid { get; set; }

            [XmlText]
            public string Text { get; set; }
        }

        [XmlRoot("recommendations")]
        public class Recommendations
        {
            [XmlElement("recommendation")]
            public List<Recommendation> Recommendation { get; set; }

            [XmlAttribute("total")]
            public int Total { get; set; }

            [XmlText]
            public string Text { get; set; }
        }

        [XmlRoot("name")]
        public class CreatorName
        {
            [XmlAttribute("id")]
            public int Id { get; set; }

            [XmlAttribute("type")]
            public string Type { get; set; }

            [XmlText]
            public string Text { get; set; }
        }

        [XmlRoot("ratings")]
        public class Ratings
        {
            [XmlElement("permanent")]
            public AnimeRating? Permanent { get; set; }

            [XmlElement("temporary")]
            public AnimeRating? Temporary { get; set; }

            [XmlElement("review")]
            public AnimeRating? Review { get; set; }
        }

        [XmlRoot("externalentity")]
        public class Externalentity
        {
            [XmlElement("identifier")]
            public List<string> Identifier { get; set; }

            [XmlElement("url")]
            public string? Url { get; set; }
        }

        [XmlRoot("resource")]
        public class Resource
        {
            [XmlElement("externalentity")]
            public Externalentity Externalentity { get; set; }

            [XmlAttribute("type")]
            public int Type { get; set; }
        }

        [XmlRoot("tag")]
        public class Tag
        {
            [XmlElement("name")]
            public string Name { get; set; }

            [XmlElement("description")]
            public string? Description { get; set; }

            [XmlElement("picurl")]
            public string? Picurl { get; set; }

            [XmlAttribute("id")]
            public int Id { get; set; }

            [XmlAttribute("parentid")]
            [MaybeNull]
            public int Parentid { get; set; }

            [XmlIgnore]
            public bool ParentidSpecified { get; set; }

            [XmlAttribute("weight")]
            public int Weight { get; set; }

            [XmlAttribute("localspoiler")]
            public bool Localspoiler { get; set; }

            [XmlAttribute("globalspoiler")]
            public bool Globalspoiler { get; set; }

            [XmlAttribute("verified")]
            public bool Verified { get; set; }

            [XmlAttribute("update")]
            public string? Update { get; set; }

            [XmlText]
            public string Text { get; set; }

            [XmlAttribute("infobox")]
            [MaybeNull]
            public bool Infobox { get; set; }

            [XmlIgnore]
            public bool InfoboxSpecified { get; set; }
        }

        public class AnimeRating
        {
            [XmlAttribute("count")]
            public int Count { get; set; }

            [XmlText]
            public float Text { get; set; }
        }

        [XmlRoot("rating")]
        public class Rating
        {
            [XmlAttribute("votes")]
            public int Votes { get; set; }

            [XmlText]
            public float Text { get; set; }
        }

        [XmlRoot("charactertype")]
        public class Charactertype
        {
            [XmlAttribute("id")]
            public int Id { get; set; }

            [XmlText]
            public string Text { get; set; }
        }

        [XmlRoot("seiyuu")]
        public class Seiyuu
        {
            [XmlAttribute("id")]
            public int Id { get; set; }

            [XmlAttribute("picture")]
            public string? Picture { get; set; }

            [XmlText]
            public string Text { get; set; }
        }

        [XmlRoot("character")]
        public class Character
        {
            [XmlElement("rating")]
            public Rating? Rating { get; set; }

            [XmlElement("name")]
            public string Name { get; set; }

            [XmlElement("gender")]
            public string Gender { get; set; }

            [XmlElement("charactertype")]
            public Charactertype Charactertype { get; set; }

            [XmlElement("description")]
            public string? Description { get; set; }

            [XmlElement("picture")]
            public string? Picture { get; set; }

            [XmlElement("seiyuu")]
            public List<Seiyuu> Seiyuu { get; set; }

            [XmlAttribute("id")]
            public int Id { get; set; }

            [XmlAttribute("type")]
            public string Type { get; set; }

            [XmlAttribute("update")]
            public string Update { get; set; }
        }

        [XmlRoot("epno")]
        public class Epno
        {
            [XmlAttribute("type")]
            public EpisodeType Type { get; set; }

            [XmlText]
            public string Text { get; set; }
        }

        [XmlRoot("episode")]
        public class Episode
        {
            [XmlElement("epno")]
            public Epno Epno { get; set; }

            [XmlElement("length")]
            public int Length { get; set; }

            [XmlElement("airdate")]
            public string? Airdate { get; set; }

            [XmlElement("rating")]
            public Rating? Rating { get; set; }

            [XmlElement("title")]
            public List<EpisodeTitle> Title { get; set; }

            [XmlElement("summary")]
            public string? Summary { get; set; }

            [XmlArray("resources")]
            [XmlArrayItem("resource")]
            public List<Resource> Resources { get; set; }

            [XmlAttribute("id")]
            public int Id { get; set; }

            [XmlAttribute("update")]
            public string Update { get; set; }

            [XmlAttribute("recap")]
            [MaybeNull]
            public bool Recap { get; set; }

            [XmlIgnore]
            public bool RecapSpecified { get; set; }
        }
    }
}
