using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Shizou.Blazor.Features;

public partial class NavMenu
{
    private bool _collapseNavMenu = true;
    private bool _isDarkMode = false;

    private string? NavMenuCssClass => _collapseNavMenu ? "collapse" : null;

    [Inject]
    private IJSRuntime JsRuntime { get; set; } = default!;

    protected override async Task OnInitializedAsync()
    {
        _isDarkMode = await JsRuntime.InvokeAsync<string>("getTheme") == "dark";
        await base.OnInitializedAsync();
    }

    private void ToggleNavMenu()
    {
        _collapseNavMenu = !_collapseNavMenu;
    }

    private async Task ToggleDarkMode()
    {
        _isDarkMode = await JsRuntime.InvokeAsync<string>("toggleTheme") == "dark";
    }
}