using Microsoft.AspNetCore.Components;

namespace Shizou.Blazor.Components.Shared;

public partial class CollapsibleSection
{
    private bool _show;

    [Parameter]
    [EditorRequired]
    public RenderFragment ChildContent { get; set; } = null!;

    [Parameter]
    [EditorRequired]
    public string Header { get; set; } = null!;

    [Parameter]
    public bool ShowDefault { get; set; }

    protected override void OnInitialized()
    {
        _show = ShowDefault;
    }
}
