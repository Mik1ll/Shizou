using Microsoft.AspNetCore.Components;

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
    private bool _opened = false;
    private string _classes = string.Empty;
    private string _displayClass = "showing";

    [CascadingParameter]
    private ToastContainer ToastDisplay { get; set; } = default!;

    [Parameter]
    [EditorRequired]
    public ToastItem ToastItem { get; set; } = default!;

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
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!_opened)
        {
            await Task.Delay(150);
            _displayClass = string.Empty;
            _opened = true;
            StateHasChanged();
        }
    }

    private void Dismiss()
    {
        ToastDisplay.RemoveToast(ToastItem);
    }
}
