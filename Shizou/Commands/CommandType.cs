using System;

namespace Shizou.Commands
{
    [Flags]
    public enum CommandType
    {
        Invalid = 0,
        Login = 1,
        Logout = 2,
        Noop = 99
    }
}
