using Blazored.Modal;
using Blazored.Modal.Services;
using Microsoft.AspNetCore.Components;

namespace Shizou.Blazor.Features.Components;

public partial class Modal
{
    private FocusTrap? _focusTrap;
    private string _classes = string.Empty;
    private string _animationClass = "fade-in";

    [CascadingParameter]
    private BlazoredModalInstance ModalInstance { get; set; } = default!;

    [Parameter]
    [EditorRequired]
    public RenderFragment ChildContent { get; set; } = default!;

    [Parameter]
    public EventCallback OnCancel { get; set; }

    [Parameter(CaptureUnmatchedValues = true)]
    public Dictionary<string, object> AdditionalAttributes { get; set; } = new();

    public async Task CloseAsync()
    {
        await CloseAsync(ModalResult.Ok());
    }

    public async Task CloseAsync(ModalResult modalResult)
    {
        _animationClass += " fade-out";
        StateHasChanged();
        await Task.Delay(400);
        await ModalInstance.CloseAsync(modalResult);
    }

    public async Task CancelAsync()
    {
        if (OnCancel.HasDelegate)
            await OnCancel.InvokeAsync();
        await CloseAsync(ModalResult.Cancel());
    }

    public async Task CancelAsync<TPayload>(TPayload payload)
    {
        if (OnCancel.HasDelegate)
            await OnCancel.InvokeAsync();
        await CloseAsync(ModalResult.Cancel(payload));
    }

    protected override void OnParametersSet()
    {
        AdditionalAttributes.Remove("class", out var addClasses);
        if (addClasses is string s)
            _classes = s;
        else
            _classes = string.Empty;
    }

    protected override void OnAfterRender(bool firstRender)
    {
        ModalInstance.FocusTrap = _focusTrap;
    }
}
