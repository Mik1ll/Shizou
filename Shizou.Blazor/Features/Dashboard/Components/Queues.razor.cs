using System.ComponentModel;
using Microsoft.AspNetCore.Components;
using Shizou.Server.CommandProcessors;

namespace Shizou.Blazor.Features.Dashboard.Components;

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

    private void OnCommandChanged(object? sender, PropertyChangedEventArgs eventArgs)
    {
        InvokeAsync(StateHasChanged);
    }
}
