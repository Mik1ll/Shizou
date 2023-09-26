using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace Shizou.Blazor.Features.Components;

public partial class SelectTable<TValue>
{
    private HashSet<SelectRow<TValue>> _rows = default!;
    private SelectRow<TValue>? _lastClicked;


    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    [Parameter]
    [EditorRequired]
    public EventCallback<List<TValue>> OnChange { get; set; }

    [Parameter(CaptureUnmatchedValues = true)]
    public Dictionary<string, object> AdditionalAttributes { get; set; } = new();

    public void AddChild(SelectRow<TValue> row)
    {
        _rows.Add(row);
    }

    public async Task RowClicked(SelectRow<TValue> row, MouseEventArgs args)
    {
        if (args.ShiftKey && _lastClicked is not null && row != _lastClicked)
        {
            var low = Math.Min(row.Index, _lastClicked.Index);
            var high = Math.Max(row.Index, _lastClicked.Index);
            foreach (var x in _rows)
                x.Active = false;
            foreach (var r in _rows.Where(r => low <= r.Index && r.Index <= high))
                r.Active = true;
        }
        else if (args.CtrlKey)
        {
            row.Active = !row.Active;
            _lastClicked = row;
        }
        else
        {
            foreach (var x in _rows)
                x.Active = false;
            row.Active = true;
            _lastClicked = row;
        }

        await Changed();
    }

    public async Task Changed()
    {
        await OnChange.InvokeAsync(_rows.Where(r => r.Active).Select(r => r.Value).ToList());
    }

    protected override void OnInitialized()
    {
        _rows = new HashSet<SelectRow<TValue>>();
    }
}
