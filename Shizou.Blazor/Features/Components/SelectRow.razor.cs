using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace Shizou.Blazor.Features.Components;

public partial class SelectRow<TValue>
{
    private HashSet<string> _classes = default!;

    public bool Active
    {
        get => _classes.Contains("table-active");
        set
        {
            if (value ? _classes.Add("table-active") : _classes.Remove("table-active"))
                StateHasChanged();
        }
    }

    [CascadingParameter]
    private SelectTable<TValue> ParentTable { get; set; } = default!;

    [Parameter]
    [EditorRequired]
    public int Index { get; set; }

    [Parameter]
    [EditorRequired]
    public TValue Value { get; set; } = default!;

    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    [Parameter(CaptureUnmatchedValues = true)]
    public Dictionary<string, object> AdditionalAttributes { get; set; } = new();

    protected override void OnInitialized()
    {
        ParentTable.AddChild(this);
        _classes = new HashSet<string>();
    }

    protected override void OnParametersSet()
    {
        var active = Active;
        AdditionalAttributes.Remove("class", out var addClasses);
        if (addClasses is string s)
            _classes = s.Split(' ', StringSplitOptions.RemoveEmptyEntries).ToHashSet();
        Active = active;
    }

    private async Task Clicked(MouseEventArgs args)
    {
        await ParentTable.RowClicked(this, args);
    }
}
