using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Shizou.Blazor.Features.Components;

public partial class Tooltip
{
    private string _classes = string.Empty;
    private ElementReference _elementReference;

    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    [Parameter(CaptureUnmatchedValues = true)]
    public Dictionary<string, object> AdditionalAttributes { get; set; } = new();

    [Parameter]
    [EditorRequired]
    public string Content { get; set; } = default!;

    [Inject]
    public IJSRuntime JsRuntime { get; set; } = default!;

    protected override Task OnAfterRenderAsync(bool firstRender)
    {
        JsRuntime.InvokeVoidAsync("loadTooltip", _elementReference);
        return base.OnAfterRenderAsync(firstRender);
    }

    protected override void OnParametersSet()
    {
        AdditionalAttributes.Remove("class", out var addClasses);
        if (addClasses is string s)
            _classes = s;
        base.OnParametersSet();
    }
}