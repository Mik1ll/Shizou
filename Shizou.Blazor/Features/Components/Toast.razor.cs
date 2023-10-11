using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Shizou.Blazor.Features.Components;

public enum ToastStyle
{
    Info,
    Success,
    Warning,
    Error
}

public record ToastItem(string Title, string Body, DateTimeOffset TriggerTime, ToastStyle ToastStyle);

public partial class Toast
{
    private ElementReference _elementReference;

    private bool _isLoaded = false;
    private string _classes = string.Empty;

    [Parameter]
    [EditorRequired]
    public ToastItem ToastItem { get; set; } = default!;

    [Inject]
    private IJSRuntime JsRuntime { get; set; } = default!;

    protected override void OnInitialized()
    {
        _classes = ToastItem.ToastStyle switch
        {
            ToastStyle.Info => "",
            ToastStyle.Success => "text-bg-success",
            ToastStyle.Warning => "text-bg-warning",
            ToastStyle.Error => "text-bg-danger",
            _ => throw new ArgumentOutOfRangeException()
        };
        base.OnInitialized();
    }


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
