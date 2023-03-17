using System;

namespace Shizou.Exceptions;

public class ProcessorPauseException : Exception
{
    public ProcessorPauseException(string pauseMessage) : base(pauseMessage)
    {
    }
}
