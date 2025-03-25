using Shizou.Data.Enums;

namespace Shizou.Data.CommandInputArgs;

public record UpdateSymbolicCollectionArgs() : CommandArgs("UpdateSymbolicCollection", CommandPriority.Low, QueueType.General);
