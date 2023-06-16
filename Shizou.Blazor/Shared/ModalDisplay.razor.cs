using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;

namespace Shizou.Blazor.Shared;

public partial class ModalDisplay
{
    public EditContext? EditContext { get; private set; }
    public ValidationMessageStore? MessageStore { get; private set; }

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
    public EventCallback<ValidationMessageStore> OnValidate { get; set; }

    [Parameter]
    public bool ShowValidationSummary { get; set; } = true;

    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    protected override void OnInitialized()
    {
        if (DialogType is ModalDialogType.Form)
        {
            EditContext = new EditContext(Model!);
            MessageStore = new ValidationMessageStore(EditContext);
            EditContext.OnValidationRequested += (_, _) => OnValidate.InvokeAsync(MessageStore);
        }
    }

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
