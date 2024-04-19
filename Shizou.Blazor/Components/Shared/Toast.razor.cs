using Microsoft.AspNetCore.Components;

namespace Shizou.Blazor.Components.Shared;

public enum ToastStyle
{
    Info,
    Success,
    Warning,
    Error
}

public record ToastItem(string Title, string Body, DateTimeOffset TriggerTime, ToastStyle ToastStyle);

public partial class Toast : IDisposable
{
    private bool _opened = false;
    private string _classes = string.Empty;
    private string _displayClass = "showing";
    private Timer? _fadeoutTimer;

    [CascadingParameter]
    private ToastContainer ToastDisplay { get; set; } = default!;

    [Parameter]
    [EditorRequired]
    public ToastItem ToastItem { get; set; } = default!;

    public void Dispose()
    {
        _fadeoutTimer?.Dispose();
    }

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
        _classes += " show";
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!_opened)
        {
            await Task.Delay(150);
            _displayClass = string.Empty;
            _fadeoutTimer = new Timer(_ =>
            {
                var currClasses = _classes.Split(' ', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries).ToHashSet();
                if (!currClasses.Add("showing"))
                {
                    currClasses = currClasses.Except(["showing", "show"]).ToHashSet();
                }
                else
                {
                    _fadeoutTimer?.Dispose();
                    _fadeoutTimer = null;
                }

                _classes = string.Join(' ', currClasses);
#pragma warning disable VSTHRD110
                InvokeAsync(StateHasChanged);
#pragma warning restore VSTHRD110
            }, null, TimeSpan.FromSeconds(20), TimeSpan.FromMilliseconds(150));
            _opened = true;
            StateHasChanged();
        }
    }

    private void Dismiss()
    {
        ToastDisplay.RemoveToast(ToastItem);
    }
}
