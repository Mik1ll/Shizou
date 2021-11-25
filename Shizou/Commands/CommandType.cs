using System;

namespace Shizou.Commands
{
    [Flags]
    public enum CommandType
    {
        Invalid = 0,
        GetFile = 1,
        Hash = 2,
        HttpGetAnime = 3,
        HttpGetMyList = 4,
        Noop = 99
    }
}
