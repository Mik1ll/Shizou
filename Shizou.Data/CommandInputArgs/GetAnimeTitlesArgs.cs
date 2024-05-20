using Shizou.Data.Enums;

namespace Shizou.Data.CommandInputArgs;

public record GetAnimeTitlesArgs() : CommandArgs("GetAnimeTitles", CommandPriority.Normal, QueueType.AniDbHttp);
