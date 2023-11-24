using Shizou.Data.Enums;

namespace Shizou.Data.CommandArgs;

public record AnimeArgs(int AnimeId, int? FetchRelationDepth = null) : CommandArgs($"Anime_{AnimeId}_{FetchRelationDepth}", CommandPriority.Normal,
    QueueType.AniDbHttp);
