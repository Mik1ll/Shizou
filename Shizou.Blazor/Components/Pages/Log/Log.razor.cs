using Microsoft.AspNetCore.Components;
using Serilog.Events;
using Shizou.Server.Controllers;
using Shizou.Server.Services;

namespace Shizou.Blazor.Components.Pages.Log;

public partial class Log : IDisposable
{
    private string _currentFileUrl = null!;

    [Inject]
    private RingBufferLogService RingBufferLogService { get; set; } = null!;

    [Inject]
    private NavigationManager NavigationManager { get; set; } = null!;

    [Inject]
    private LinkGenerator LinkGenerator { get; set; } = null!;

    public void Dispose()
    {
        RingBufferLogService.OnChange -= OnChange;
    }

    protected override void OnInitialized()
    {
        RingBufferLogService.OnChange += OnChange;
        var baseUri = new Uri(NavigationManager.BaseUri);
        _currentFileUrl = LinkGenerator.GetUriByAction(nameof(Logs.GetCurrentFile), nameof(Logs), null, baseUri.Scheme, new HostString(baseUri.Authority),
            new PathString(baseUri.AbsolutePath)) ?? throw new ArgumentException("Failed to generate file download uri");
    }

    private void OnChange(object? o, EventArgs logEventArgs) => _ = InvokeAsync(StateHasChanged);

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
