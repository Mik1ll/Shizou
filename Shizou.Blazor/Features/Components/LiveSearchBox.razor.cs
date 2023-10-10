using Microsoft.AspNetCore.Components;
using Timer = System.Timers.Timer;

namespace Shizou.Blazor.Features.Components;

public partial class LiveSearchBox
{
    private List<(int, string)> _results = new();
    private string? _query;
    private Timer _searchTimer = default!;

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
                // display error toast
            }
            else
            {
                _results = res;
                await InvokeAsync(StateHasChanged);
            }
        };
    }

    private void OnInputChanged(ChangeEventArgs e)
    {
        _query = (string?)e.Value;
        _searchTimer.Stop();
        _searchTimer.Start();
    }
}
