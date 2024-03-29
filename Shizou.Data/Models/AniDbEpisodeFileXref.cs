﻿using Microsoft.EntityFrameworkCore;

namespace Shizou.Data.Models;

[PrimaryKey(nameof(AniDbEpisodeId), nameof(AniDbFileId))]
public class AniDbEpisodeFileXref
{
    public required int AniDbEpisodeId { get; set; }
    public AniDbEpisode AniDbEpisode { get; set; } = default!;
    public required int AniDbFileId { get; set; }
    public AniDbFile AniDbFile { get; set; } = default!;
}
