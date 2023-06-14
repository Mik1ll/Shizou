using System.ComponentModel;
using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;
using Shizou.CommandProcessors;
using ShizouData.Database;

namespace ShizouBlazor.Shared;

public partial class Queues : IDisposable
{
    private readonly List<CommandProcessor> _processors = new();

    [Inject]
    private IDbContextFactory<ShizouContext> ContextFactory { get; set; } = default!;

    [Inject]
    private ILogger<Queues> Logger { get; set; } = default!;

    [Inject]
    private IServiceProvider ServiceProvider { get; set; } = default!;

    public void Dispose()
    {
        foreach (var processor in _processors)
            processor.PropertyChanged -= OnCommandChanged;
    }

    protected override void OnInitialized()
    {
        _processors.AddRange(ServiceProvider.GetServices<CommandProcessor>());
        foreach (var processor in _processors)
            processor.PropertyChanged += OnCommandChanged;
    }

    private void OnCommandChanged(object? sender, PropertyChangedEventArgs eventArgs)
    {
        InvokeAsync(StateHasChanged);
    }
}
