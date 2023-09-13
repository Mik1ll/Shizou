using Microsoft.AspNetCore.Components;

namespace Shizou.Blazor.Features.Components;

public partial class ToastDisplay
{
    private readonly List<ToastItem> _toasts = new();

    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    public void AddToast(string title, string body)
    {
        _toasts.Add(new ToastItem(title, body, DateTimeOffset.Now));
        StateHasChanged();
    }
}