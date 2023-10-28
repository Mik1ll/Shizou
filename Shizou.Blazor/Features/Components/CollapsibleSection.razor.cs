using Microsoft.AspNetCore.Components;

namespace Shizou.Blazor.Features.Components;

public partial class CollapsibleSection
{
    private bool _show;

    [Parameter]
    [EditorRequired]
    public RenderFragment ChildContent { get; set; } = default!;

    [Parameter]
    [EditorRequired]
    public string Header { get; set; } = default!;

    [Parameter]
    public bool ShowDefault { get; set; }

    protected override void OnInitialized()
    {
        _show = ShowDefault;
    }
}
