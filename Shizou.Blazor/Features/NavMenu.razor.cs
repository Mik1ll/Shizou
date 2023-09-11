using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Shizou.Blazor.Features;

public partial class NavMenu
{
    private bool _collapseNavMenu = true;

    private string? NavMenuCssClass => _collapseNavMenu ? "collapse" : null;

    [Inject]
    private IJSRuntime JsRuntime { get; set; } = default!;

    private void ToggleNavMenu()
    {
        _collapseNavMenu = !_collapseNavMenu;
    }

    private async Task ToggleDarkMode()
    {
        await JsRuntime.InvokeVoidAsync("toggleTheme");
    }
}