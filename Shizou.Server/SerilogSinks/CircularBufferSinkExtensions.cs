using Serilog;
using Serilog.Configuration;
using Serilog.Core;
using Serilog.Events;
using Serilog.Formatting.Display;

namespace Shizou.Server.SerilogSinks;

public static class CircularBufferSinkExtensions
{
    public static LoggerConfiguration CircularBuffer(
        this LoggerSinkConfiguration sinkConfiguration,
        string outputTemplate,
        LogEventLevel restrictedToMinimumLevel = LevelAlias.Minimum,
        int bufferSize = 500
    )
    {
        var inst = CircularBufferSink.Instance.Value;
        inst.TextFormatter = new MessageTemplateTextFormatter(outputTemplate);
        inst.QueueSize = bufferSize;
        return sinkConfiguration.Sink(inst, LevelAlias.Minimum, new LoggingLevelSwitch(restrictedToMinimumLevel));
    }
}
