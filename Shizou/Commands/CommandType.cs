using System;

namespace Shizou.Commands
{
    [Flags]
    public enum CommandType
    {
        Invalid = 0,
        GetFile = 1,
        Noop = 99
    }
}
