using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Shizou.Blazor.Features.Components;

public record ToastItem(string Title, string Body, DateTimeOffset TriggerTime);

public partial class Toast
{
    private ElementReference _elementReference;

    private bool _isLoaded = false;

    [Parameter]
    [EditorRequired]
    public ToastItem ToastItem { get; set; } = default!;

    [Inject]
    public IJSRuntime JsRuntime { get; set; } = default!;


    protected override Task OnAfterRenderAsync(bool firstRender)
    {
        if (!_isLoaded)
        {
            JsRuntime.InvokeVoidAsync("loadToast", _elementReference);
            _isLoaded = true;
        }

        return base.OnAfterRenderAsync(firstRender);
    }
}