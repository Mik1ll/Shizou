using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Shizou.Blazor.Features.Components;

public partial class Tooltip
{
    private string _classes = string.Empty;
    private ElementReference _elementReference;
    private bool _isLoaded = false;

    [Inject]
    private IJSRuntime JsRuntime { get; set; } = default!;

    [Parameter]
    [EditorRequired]
    public string Content { get; set; } = default!;

    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    [Parameter(CaptureUnmatchedValues = true)]
    public Dictionary<string, object> AdditionalAttributes { get; set; } = new();

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!_isLoaded)
        {
            //await JsRuntime.InvokeVoidAsync("loadTooltip", _elementReference);
            _isLoaded = true;
        }
    }

    protected override void OnParametersSet()
    {
        AdditionalAttributes.Remove("class", out var addClasses);
        if (addClasses is string s)
            _classes = s;
    }
}
