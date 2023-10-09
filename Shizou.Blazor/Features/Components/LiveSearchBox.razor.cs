using Microsoft.AspNetCore.Components;

namespace Shizou.Blazor.Features.Components;

public partial class LiveSearchBox
{
    private List<(int, string)>? _results;

    [Parameter]
    [EditorRequired]
    public Func<string, Task<List<(int, string)>?>> GetResults { get; set; } = default!;

    [Parameter]
    public int? Selected { get; set; }

    [Parameter]
    public EventCallback<int?> SelectedChanged { get; set; }

    private async Task OnInputChanged(ChangeEventArgs e)
    {
        var query = (string)(e.Value ?? string.Empty);
        _results = await GetResults(query);
    }
}
