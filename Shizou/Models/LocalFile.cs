﻿using System;
using Microsoft.EntityFrameworkCore;

namespace Shizou.Models
{
    [Index(nameof(Ed2K), IsUnique = true)]
    [Index(nameof(Signature), IsUnique = true)]
    [Index(nameof(ImportFolderId), nameof(PathTail), IsUnique = true)]
    public sealed class LocalFile : IEntity
    {
        public int Id { get; set; }
        public string Ed2K { get; set; } = null!;
        public string Crc { get; set; } = null!;
        public long FileSize { get; set; }
        public string Signature { get; set; } = null!;
        public bool Ignored { get; set; }
        public string PathTail { get; set; } = null!;

        public DateTime? Updated { get; set; }

        public int ImportFolderId { get; set; }
        public ImportFolder ImportFolder { get; set; } = null!;
        public int? ManualLinkEpisodeId { get; set; }
        public AniDbEpisode? ManualLinkEpisode { get; set; }
        public int? AniDbFileId { get; set; }
        public AniDbFile? AniDbFile { get; set; }
    }
}