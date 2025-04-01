using System.ComponentModel;
using Blazored.Modal.Services;
using Microsoft.AspNetCore.Components;
using Shizou.Server.CommandProcessors;

namespace Shizou.Blazor.Components.Shared;

public partial class QueuesModal : ComponentBase, IDisposable
{
    private readonly Dictionary<CommandProcessor, bool> _nextExpanded = new();

    private readonly Dictionary<CommandProcessor, bool> _prevExpanded = new();

    [Inject]
    private IEnumerable<CommandProcessor> Processors { get; set; } = null!;

    [CascadingParameter]
    private IModalService ModalService { get; set; } = null!;

    public void Dispose()
    {
        foreach (var processor in Processors)
            processor.PropertyChanged -= OnCommandChanged;
    }

    protected override void OnInitialized()
    {
        foreach (var processor in Processors)
        {
            processor.PropertyChanged += OnCommandChanged;
            _nextExpanded[processor] = false;
            _prevExpanded[processor] = false;
        }
    }

    private void PauseAll()
    {
        foreach (var p in Processors) p.Pause();
    }

    private void UnpauseAll()
    {
        foreach (var p in Processors) p.Unpause();
    }

    private void ClearAll()
    {
        foreach (var p in Processors) p.ClearQueue();
    }

    private void TogglePause(CommandProcessor processor)
    {
        if (processor.Paused)
            processor.Unpause();
        else
            processor.Pause();
    }

#pragma warning disable VSTHRD100
    private async void OnCommandChanged(object? sender, PropertyChangedEventArgs e)
#pragma warning restore VSTHRD100
    {
        await InvokeAsync(StateHasChanged);
    }
}
