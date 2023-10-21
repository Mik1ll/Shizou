using Blazored.Modal;
using Microsoft.AspNetCore.Components;

namespace Shizou.Blazor.Features.Components;

public partial class ModalWrapper
{
    private FocusTrap? _focusTrap;
    private string _classes = string.Empty;

    [CascadingParameter]
    private BlazoredModalInstance ModalInstance { get; set; } = default!;

    [Parameter]
    [EditorRequired]
    public RenderFragment ChildContent { get; set; } = default!;

    [Parameter]
    public EventCallback Cancel { get; set; }

    [Parameter(CaptureUnmatchedValues = true)]
    public Dictionary<string, object> AdditionalAttributes { get; set; } = new();

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

    private async Task CancelWrapped()
    {
        if (Cancel.HasDelegate)
            await Cancel.InvokeAsync();
        else
            await ModalInstance.CancelAsync();
    }
}
