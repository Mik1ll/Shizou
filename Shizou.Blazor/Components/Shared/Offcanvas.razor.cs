using Microsoft.AspNetCore.Components;

namespace Shizou.Blazor.Components.Shared;

public partial class Offcanvas
{
    private bool _open;
    private string _displayClass = string.Empty;

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

    [Parameter]
    [EditorRequired]
    public OffcanvasDirection Direction { get; set; }

    [Parameter]
    [EditorRequired]
    public bool UseButton { get; set; }

    [Parameter]
    public RenderFragment ChildContent { get; set; } = default!;

    public async Task ToggleAsync()
    {
        if (_open)
            await CloseAsync();
        else
            await OpenAsync();
    }

    public async Task OpenAsync()
    {
        if (!_open)
        {
            _open = true;
            StateHasChanged();
            _displayClass = "showing";
            await Task.Delay(150);
            _displayClass = "show";
            StateHasChanged();
        }
    }

    public async Task CloseAsync()
    {
        if (_open)
        {
            _displayClass = "hiding";
            await Task.Delay(300);
            _displayClass = string.Empty;
            _open = false;
        }
    }
}

public enum OffcanvasDirection
{
    Start,
    End
}
