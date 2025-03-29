using System;
using System.Collections.Concurrent;
using Serilog.Core;
using Serilog.Events;
using Serilog.Formatting.Display;

namespace Shizou.Server.Services;

public class RingBufferLogService : ILogEventSink
{
    private readonly ConcurrentQueue<LogEvent> _logEvents = new();

    public RingBufferLogService(string outputTemplate,
        LogEventLevel restrictedToMinimumLevel = LevelAlias.Minimum,
        int bufferSize = 500)
    {
        TextFormatter = new MessageTemplateTextFormatter(outputTemplate);
        LogLevelSwitch = new LoggingLevelSwitch(restrictedToMinimumLevel);
        BufferSize = bufferSize;
    }

    public event EventHandler? OnChange;

    public LoggingLevelSwitch LogLevelSwitch { get; }
    public int BufferSize { get; }

    public MessageTemplateTextFormatter TextFormatter { get; }

    public LogEvent[] Snapshot() => _logEvents.ToArray();

    public void Emit(LogEvent logEvent)
    {
        _logEvents.Enqueue(logEvent);
        while (_logEvents.Count > BufferSize)
            _logEvents.TryDequeue(out _);

        OnChange?.Invoke(this, EventArgs.Empty);
    }
}
