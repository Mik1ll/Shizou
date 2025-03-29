using Microsoft.AspNetCore.Components;
using Serilog.Events;
using Shizou.Server.Services;

namespace Shizou.Blazor.Components.Pages.Utilities.Logs;

public partial class Logs : IDisposable
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
}
