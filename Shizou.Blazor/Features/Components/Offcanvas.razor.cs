using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Shizou.Blazor.Features.Components;

public partial class Offcanvas
{
    private bool _isLoaded;
    private bool _open;
    private ElementReference _elementReference;

    private string OffCanvasDirectionClass =>
        Direction switch
        {
            OffcanvasDirection.Start => "offcanvas-start",
            OffcanvasDirection.End => "offcanvas-end",
            _ => throw new ArgumentOutOfRangeException()
        };

    private string ButtonDirectionClass => Direction switch
    {
        OffcanvasDirection.Start => "rounded-end-pill border-start-0 ps-1 pe-2",
        OffcanvasDirection.End => "rounded-start-pill border-end-0 pe-1 ps-2",
        _ => throw new ArgumentOutOfRangeException()
    };

    private string ButtonDirectionStyle => Direction switch
    {
        OffcanvasDirection.Start => "right: calc((var(--bs-offcanvas-padding-x) + .8rem) * -1)",
        OffcanvasDirection.End => "left: calc((var(--bs-offcanvas-padding-x) + .8rem) * -1)",
        _ => throw new ArgumentOutOfRangeException()
    };
    
    [Inject]
    private IJSRuntime JsRuntime { get; set; } = default!;

    [Parameter]
    [EditorRequired]
    public OffcanvasDirection Direction { get; set; }

    [Parameter]
    public RenderFragment ChildContent { get; set; } = default!;

    private async Task Toggle()
    {
        _open = !_open;
        if (_open)
            await JsRuntime.InvokeVoidAsync("showOffcanvas", _elementReference);
        else
            await JsRuntime.InvokeVoidAsync("hideOffcanvas", _elementReference);
    }
}

public enum OffcanvasDirection
{
    Start,
    End
}
