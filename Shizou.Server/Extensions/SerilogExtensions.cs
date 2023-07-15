using System;
using Serilog.Context;
using Serilog.Events;

namespace Shizou.Server.Extensions;

public static class SerilogExtensions
{
    private const string SuppressLoggingProperty = "SuppressLogging";
    private const string SuppressLoggingSourceProperty = "SuppressLogggingSource";

    public static IDisposable SuppressLogging(string? sourceContext = null)
    {
        var stackBottom = LogContext.PushProperty(SuppressLoggingProperty, true);
        if (sourceContext is not null)
            LogContext.PushProperty(SuppressLoggingSourceProperty, sourceContext);
        return stackBottom;
    }

    public static bool IsSuppressed(this LogEvent logEvent)
    {
        var containsSuppressed = logEvent.Properties
            .TryGetValue(SuppressLoggingProperty, out var suppressedProperty);

        if (!containsSuppressed)
            return false;

        var containsSuppressSource = logEvent.Properties.TryGetValue(SuppressLoggingSourceProperty, out var suppressSourceProperty);

        // remove suppression property from logs
        logEvent.RemovePropertyIfPresent(SuppressLoggingProperty);
        logEvent.RemovePropertyIfPresent(SuppressLoggingSourceProperty);

        var containsSource = logEvent.Properties.TryGetValue("SourceContext", out var sourceProperty);

        if (suppressedProperty is ScalarValue { Value: bool isSuppressed } &&
            (!containsSuppressSource || (containsSource && suppressSourceProperty is ScalarValue { Value: string suppressSource } &&
                                         sourceProperty is ScalarValue { Value: string source } && suppressSource == source)))
            return isSuppressed;

        return false;
    }
}
