using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;

namespace Shizou.Blazor.Shared;

public partial class ModalDisplay
{
    [Parameter]
    public object? Model { get; set; }

    [Parameter]
    [EditorRequired]
    public ModalDialogType DialogType { get; set; }

    [Parameter]
    [EditorRequired]
    public string Title { get; set; } = null!;

    [Parameter]
    [EditorRequired]
    public EventCallback<bool> OnClose { get; set; }

    [Parameter]
    public bool ShowValidationSummary { get; set; } = true;

    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    public override Task SetParametersAsync(ParameterView parameters)
    {
        parameters.SetParameterProperties(this);
        if (DialogType is ModalDialogType.Form && Model is null)
            throw new ArgumentNullException($"{nameof(ModalDisplay)} requires a {nameof(Model)} when type is {Enum.GetName(ModalDialogType.Form)}");
        return base.SetParametersAsync(parameters);
    }

    private Task ModalCancel()
    {
        return OnClose.InvokeAsync(false);
    }

    private async Task ModalOk()
    {
        await OnClose.InvokeAsync(true);
    }

    public enum ModalDialogType
    {
        Ok,
        OkCancel,
        DeleteCancel,
        Form
    }
}
