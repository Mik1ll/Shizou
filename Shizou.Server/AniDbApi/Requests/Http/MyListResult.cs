﻿using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Xml.Serialization;
using Shizou.Data.Enums;
using Shizou.Server.AniDbApi.Requests.Http.SubElements;

// ReSharper disable RedundantNullableFlowAttribute

#pragma warning disable CS8618


namespace Shizou.Server.AniDbApi.Requests.Http
{
    [XmlRoot("mylist")]
    public class MyListResult
    {
        [XmlElement("mylistitem")]
        public List<MyListItemResult> MyListItems { get; set; }

        [XmlAttribute("uid")]
        public int Uid { get; set; }
    }

    namespace SubElements
    {
        public class MyListItemResult
        {
            [XmlElement("state")]
            public MyListState State { get; set; }

            [XmlElement("filestate")]
            [MaybeNull]
            public MyListFileState FileState { get; set; }

            [XmlIgnore]
            public bool FileStateSpecified { get; set; }

            [XmlAttribute("id")]
            public int Id { get; set; }

            [XmlAttribute("aid")]
            public int Aid { get; set; }

            [XmlAttribute("eid")]
            public int Eid { get; set; }

            [XmlAttribute("fid")]
            public int Fid { get; set; }

            /// <summary>
            ///     Only returns date, should be UTC
            /// </summary>
            [XmlAttribute("updated")]
            public string Updated { get; set; }

            [XmlAttribute("startpercentage")]
            [MaybeNull]
            public int StartPercentage { get; set; }

            [XmlIgnore]
            public bool StartPercentageSpecified { get; set; }

            [XmlAttribute("endpercentage")]
            [MaybeNull]
            public int EndPercentage { get; set; }

            [XmlIgnore]
            public bool EndPercentageSpecified { get; set; }

            [XmlAttribute("viewdate")]
            public string? Viewdate;
        }
    }
}
