using System;

namespace Shizou.Enums
{
    [Flags]
    public enum CommandType
    {
        Invalid = -1,
        Noop = 99,
        AniDbUdp = 1 << 7,
        AniDbHttp = 1 << 8
    }
}
