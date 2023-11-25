using Shizou.Blazor.Features.Components;

namespace Shizou.Blazor.Services;

public class ToastService
{
    public event Action<ToastItem>? OnShow;

    public void ShowInfo(string title, string body) => Show(title, body, ToastStyle.Info);
    public void ShowWarn(string title, string body) => Show(title, body, ToastStyle.Warning);
    public void ShowSuccess(string title, string body) => Show(title, body, ToastStyle.Success);
    public void ShowError(string title, string body) => Show(title, body, ToastStyle.Error);

    private void Show(string title, string body, ToastStyle toastStyle)
    {
        OnShow?.Invoke(new ToastItem(title, body, DateTimeOffset.Now, toastStyle));
    }
}
