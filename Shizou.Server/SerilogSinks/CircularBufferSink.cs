using System;
using System.Collections.Concurrent;
using Serilog.Core;
using Serilog.Events;
using Serilog.Formatting;
using Serilog.Formatting.Display;

namespace Shizou.Server.SerilogSinks;

public class CircularBufferSink : ILogEventSink
{
    public static readonly Lazy<CircularBufferSink> Instance = new(() => new CircularBufferSink());
    private readonly ConcurrentQueue<LogEvent> _logEvents = new();
    public event EventHandler? OnChange;

    public ITextFormatter TextFormatter { get; set; } =
        new MessageTemplateTextFormatter("{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}");

    public int QueueSize { get; set; } = 500;

    public LogEvent[] Snapshot() => _logEvents.ToArray();

    public void Emit(LogEvent logEvent)
    {
        _logEvents.Enqueue(logEvent);
        while (_logEvents.Count > QueueSize)
            _logEvents.TryDequeue(out _);

        OnChange?.Invoke(this, EventArgs.Empty);
    }
}
