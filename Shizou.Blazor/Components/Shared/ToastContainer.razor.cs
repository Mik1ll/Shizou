using Microsoft.AspNetCore.Components;
using Shizou.Blazor.Services;

namespace Shizou.Blazor.Components.Shared;

public partial class ToastContainer
{
    private readonly List<ToastItem> _toasts = new();

    [Inject]
    private ToastService ToastService { get; set; } = default!;

    public void RemoveToast(ToastItem toast)
    {
        _toasts.Remove(toast);
        StateHasChanged();
    }

    protected override void OnInitialized()
    {
        ToastService.OnShow += AddToast;
    }

    private void AddToast(ToastItem toastItem)
    {
        _toasts.Add(toastItem);
        StateHasChanged();
    }
}
