using Shizou.Data.Enums;

namespace Shizou.Data.CommandInputArgs;

public record GetAnimeByEpisodeIdArgs(int EpisodeId) : CommandArgs($"GetAnimeByEpId_{EpisodeId}", CommandPriority.Low, QueueType.AniDbUdp);
