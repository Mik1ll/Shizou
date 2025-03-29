using Serilog.Events;
using Shizou.Server.SerilogSinks;

namespace Shizou.Blazor.Components.Pages.Utilities.Logs;

public partial class Logs : IDisposable
{
    private readonly CircularBufferSink _bufferSink = CircularBufferSink.Instance.Value;

    protected override void OnInitialized()
    {
        _bufferSink.OnChange += OnChange;
    }

    public void Dispose()
    {
        _bufferSink.OnChange -= OnChange;
    }

    private void OnChange(object? o, EventArgs logEventArgs) => InvokeAsync(StateHasChanged);

    private string GetText(LogEvent logEvent)
    {
        var output = new StringWriter();
        _bufferSink.TextFormatter.Format(logEvent, output);
        return output.ToString();
    }
}
