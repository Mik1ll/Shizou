using System.ComponentModel;
using Blazored.Modal.Services;
using Microsoft.AspNetCore.Components;
using Shizou.Blazor.Components.Shared;
using Shizou.Server.CommandProcessors;

namespace Shizou.Blazor.Components.Layout.Components;

public partial class QueueBadge : ComponentBase, IDisposable
{
    private string _extraClasses = string.Empty;

    [Inject]
    private IEnumerable<CommandProcessor> Processors { get; set; } = null!;

    [Inject]
    private IModalService ModalService { get; set; } = null!;

    [Parameter(CaptureUnmatchedValues = true)]
    public Dictionary<string, object>? AdditionalAttributes { get; set; }

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

    protected override void OnParametersSet()
    {
        if (AdditionalAttributes is not null && AdditionalAttributes.Remove("class", out var addClasses))
            _extraClasses = addClasses as string ?? string.Empty;
    }

    private async Task OpenQueueAsync()
    {
        await ModalService.Show<QueuesModal>().Result;
    }

    private string GetQueueBadgeColor()
    {
        return !Processors.Any(p => p.Paused) ? "primary" : Processors.All(p => p.Paused) ? "danger" : "warning";
    }


    private void OnCommandChanged(object? sender, PropertyChangedEventArgs e) => _ = InvokeAsync(StateHasChanged);
}
