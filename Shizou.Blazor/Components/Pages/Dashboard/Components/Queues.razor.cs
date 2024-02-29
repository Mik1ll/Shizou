using System.ComponentModel;
using Microsoft.AspNetCore.Components;
using Shizou.Server.CommandProcessors;

namespace Shizou.Blazor.Components.Pages.Dashboard.Components;

public partial class Queues : IDisposable
{
    [Inject]
    private IEnumerable<CommandProcessor> Processors { get; set; } = default!;

    public void Dispose()
    {
        foreach (var processor in Processors)
            processor.PropertyChanged -= OnCommandChanged;
    }

    protected override void OnInitialized()
    {
        foreach (var processor in Processors)
            processor.PropertyChanged += OnCommandChanged;
    }

#pragma warning disable VSTHRD100
    private async void OnCommandChanged(object? sender, PropertyChangedEventArgs e)
#pragma warning restore VSTHRD100
    {
        await InvokeAsync(StateHasChanged);
    }
}
