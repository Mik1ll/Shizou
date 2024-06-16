using System.Timers;
using Microsoft.AspNetCore.Components;
using Shizou.Blazor.Services;
using Timer = System.Timers.Timer;

namespace Shizou.Blazor.Components.Shared;

public partial class LiveSearchBox
{
    private List<(int, string)> _results = new();
    private string? _query;
    private Timer _searchTimer = default!;

    [Inject]
    private ToastService ToastService { get; set; } = default!;

    [Parameter]
    [EditorRequired]
    public Func<string, Task<List<(int, string)>?>> GetResults { get; set; } = default!;

    [Parameter]
    public int? Selected { get; set; }

    [Parameter]
    public EventCallback<int?> SelectedChanged { get; set; }

    [Parameter]
    public string? PlaceholderText { get; set; }

    [Parameter]
    public bool? Disabled { get; set; }

    [Parameter]
    public bool ShowSelect { get; set; } = true;

    [Parameter]
    public EventCallback<List<(int, string)>?> OnResultsRetrieved { get; set; }

    protected override void OnInitialized()
    {
        _searchTimer = new Timer(TimeSpan.FromMilliseconds(300))
        {
            AutoReset = false
        };
        _searchTimer.Elapsed += OnSearchTimerElapsed;
    }

#pragma warning disable VSTHRD100
    private async void OnSearchTimerElapsed(object? o, ElapsedEventArgs elapsedEventArgs)
#pragma warning restore VSTHRD100
    {
        if (string.IsNullOrWhiteSpace(_query))
        {
            _results.Clear();
            await InvokeAsync(StateHasChanged);
            await InvokeAsync(() => OnResultsRetrieved.InvokeAsync(null));
            return;
        }

        var res = await GetResults(_query);
        if (res is null)
        {
            ToastService.ShowError("Search failed", "Search was unable to retrieve results");
        }
        else
        {
            _results = res;
            await InvokeAsync(StateHasChanged);
            await InvokeAsync(() => OnResultsRetrieved.InvokeAsync(_results));
        }
    }

    private async Task UpdateSelectedAsync(int? selected)
    {
        Selected = selected;
        await SelectedChanged.InvokeAsync(Selected);
    }

    private async Task OnSelectChangedAsync(ChangeEventArgs e)
    {
        var selected = int.Parse(((string[])e.Value!).Single());
        await UpdateSelectedAsync(selected);
        _query = _results.First(r => r.Item1 == selected).Item2.ToString();
        _results.Clear();
    }

    private async Task OnInputChangedAsync(ChangeEventArgs e)
    {
        await UpdateSelectedAsync(null);
        _query = (string?)e.Value;
        _searchTimer.Stop();
        _searchTimer.Start();
    }
}
