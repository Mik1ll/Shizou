using Serilog;
using Serilog.Configuration;
using Serilog.Events;
using Shizou.Server.Services;

namespace Shizou.Server.SerilogSinks;

public static class RingBufferSinkExtensions
{
    public static LoggerConfiguration RingBuffer(this LoggerSinkConfiguration sinkConfiguration, RingBufferLogService ringBufferLogService) =>
        sinkConfiguration.Sink(ringBufferLogService, LevelAlias.Minimum, ringBufferLogService.LogLevelSwitch);
}
