using Shizou.Blazor.Features.Components;

namespace Shizou.Blazor.Services;

public class ToastService
{
    public event Action<ToastItem>? OnShow;

    public void AddToast(string title, string body, ToastStyle toastStyle)
    {
        OnShow?.Invoke(new ToastItem(title, body, DateTimeOffset.Now, toastStyle));
    }
}
