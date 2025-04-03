using System.ComponentModel;
using Blazored.Modal.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.JSInterop;
using Shizou.Blazor.Components.Shared;
using Shizou.Server.CommandProcessors;

namespace Shizou.Blazor.Components.Layout;

public partial class NavMenu : IDisposable
{
    private bool _collapsed = true;
    private bool _expandFileUtils = false;
    private string _theme = "auto";
    private Dictionary<string, string> _themeIconClasses = new() { { "light", "bi-sun-fill" }, { "dark", "bi-moon-stars-fill" }, { "auto", "bi-circle-half" } };
    private IJSObjectReference? _themeModule;
    private string _currentUrl = null!;

    private string? NavMenuCssClass => _collapsed ? null : "show";

    [Inject]
    private IJSRuntime JsRuntime { get; set; } = null!;

    [Inject]
    private NavigationManager NavigationManager { get; set; } = null!;

    [Inject]
    private IModalService ModalService { get; set; } = null!;

    [Inject]
    private IEnumerable<CommandProcessor> Processors { get; set; } = null!;

    public void Dispose()
    {
        NavigationManager.LocationChanged -= OnLocationChanged;
        foreach (var processor in Processors)
            processor.PropertyChanged -= OnCommandChanged;
    }


    protected override void OnInitialized()
    {
        _currentUrl = NavigationManager.ToBaseRelativePath(NavigationManager.Uri);
        NavigationManager.LocationChanged += OnLocationChanged;
        foreach (var processor in Processors)
            processor.PropertyChanged += OnCommandChanged;
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            _themeModule = await JsRuntime.InvokeAsync<IJSObjectReference>("import", "/js/theme.js");
            _theme = await _themeModule.InvokeAsync<string>("getStoredTheme");
            StateHasChanged();
        }
    }

#pragma warning disable VSTHRD100
    private async void OnCommandChanged(object? sender, PropertyChangedEventArgs e)
#pragma warning restore VSTHRD100
    {
        try
        {
            await InvokeAsync(StateHasChanged);
        }
        catch (Exception)
        {
            // ignored
        }
    }

    private void ToggleCollapse() => _collapsed = !_collapsed;

    private void ToggleFileUtilDropdown() => _expandFileUtils = !_expandFileUtils;

    private void CloseFileUtilDropdown() => _expandFileUtils = false;

    private async Task ToggleDarkModeAsync()
    {
        if (_themeModule != null)
            _theme = await _themeModule.InvokeAsync<string>("cycleTheme");
    }

    private void OnLocationChanged(object? sender, LocationChangedEventArgs e)
    {
        _currentUrl = NavigationManager.ToBaseRelativePath(e.Location);
        StateHasChanged();
    }

    private async Task OpenQueueAsync()
    {
        await ModalService.Show<QueuesModal>().Result;
    }
}
