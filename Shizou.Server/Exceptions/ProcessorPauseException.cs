using System;

namespace Shizou.Server.Exceptions;

public class ProcessorPauseException : Exception
{
    public ProcessorPauseException(string pauseMessage) : base(pauseMessage)
    {
    }
}
