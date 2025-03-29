using Microsoft.AspNetCore.Components;
using Serilog.Events;
using Shizou.Server.Services;

namespace Shizou.Blazor.Components.Pages.Log;

public partial class Log : IDisposable
{
    [Inject]
    private RingBufferLogService RingBufferLogService { get; set; } = null!;

    protected override void OnInitialized()
    {
        RingBufferLogService.OnChange += OnChange;
    }

    public void Dispose()
    {
        RingBufferLogService.OnChange -= OnChange;
    }

    private void OnChange(object? o, EventArgs logEventArgs) => InvokeAsync(StateHasChanged);

    private string GetText(LogEvent logEvent)
    {
        var output = new StringWriter();
        RingBufferLogService.TextFormatter.Format(logEvent, output);
        return output.ToString();
    }

    private string GetLogLevelClass(LogEvent logEvent)
    {
        return logEvent.Level switch
        {
            LogEventLevel.Error => "text-bg-danger",
            LogEventLevel.Warning => "text-bg-warning",
            LogEventLevel.Information => "text-bg-info",
            _ => "text-bg-secondary",
        };
    }

    private void ClearLogs()
    {
        RingBufferLogService.Clear();
        StateHasChanged();
    }
}
