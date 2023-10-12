﻿using Microsoft.AspNetCore.Components;
using Timer = System.Timers.Timer;

namespace Shizou.Blazor.Features.Components;

public partial class LiveSearchBox
{
    private List<(int, string)> _results = new();
    private string? _query;
    private Timer _searchTimer = default!;

    [CascadingParameter]
    private ToastDisplay ToastDisplay { get; set; } = default!;

    [Parameter]
    [EditorRequired]
    public Func<string, Task<List<(int, string)>?>> GetResults { get; set; } = default!;

    [Parameter]
    public int? Selected { get; set; }

    [Parameter]
    public EventCallback<int?> SelectedChanged { get; set; }

    protected override void OnInitialized()
    {
        _searchTimer = new Timer(TimeSpan.FromMilliseconds(500))
        {
            AutoReset = false
        };
        _searchTimer.Elapsed += async (_, _) =>
        {
            if (string.IsNullOrWhiteSpace(_query))
            {
                _results.Clear();
                await InvokeAsync(StateHasChanged);
                return;
            }

            var res = await GetResults(_query);
            if (res is null)
            {
                ToastDisplay.AddToast("Search failed", "Search was unable to retrieve results", ToastStyle.Error);
            }
            else
            {
                _results = res;
                await InvokeAsync(StateHasChanged);
            }
        };
    }

    private async Task UpdateSelected(int? selected)
    {
        Selected = selected;
        await SelectedChanged.InvokeAsync(Selected);
    }

    private async Task OnSelectChanged(ChangeEventArgs e)
    {
        var selected = int.Parse(((string[])e.Value!).Single());
        await UpdateSelected(selected);
        _query = _results.First(r => r.Item1 == selected).Item2.ToString();
        _results.Clear();
    }

    private async Task OnInputChanged(ChangeEventArgs e)
    {
        await UpdateSelected(null);
        _query = (string?)e.Value;
        _searchTimer.Stop();
        _searchTimer.Start();
    }
}