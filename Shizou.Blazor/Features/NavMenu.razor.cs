using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Shizou.Blazor.Features;

public partial class NavMenu
{
    private bool _collapsed = true;
    private bool _isDarkMode = false;

    private string? NavMenuCssClass => _collapsed ? null : "show";

    [Inject]
    private IJSRuntime JsRuntime { get; set; } = default!;

    protected override async Task OnInitializedAsync()
    {
        _isDarkMode = await JsRuntime.InvokeAsync<string>("getTheme") == "dark";
    }

    private void ToggleCollapse()
    {
        _collapsed = !_collapsed;
    }

    private async Task ToggleDarkModeAsync()
    {
        _isDarkMode = await JsRuntime.InvokeAsync<string>("toggleTheme") == "dark";
    }
}
