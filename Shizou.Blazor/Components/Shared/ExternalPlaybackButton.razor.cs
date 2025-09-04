using System.Dynamic;
using Blazored.Modal.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Identity;
using Microsoft.JSInterop;
using Shizou.Server.Controllers;

namespace Shizou.Blazor.Components.Shared;

public partial class ExternalPlaybackButton : ComponentWithExtraClasses
{
    private bool _schemeHandlerInstalled;
    private string _externalPlaybackUri = string.Empty;

    [Inject]
    private LinkGenerator LinkGenerator { get; set; } = null!;

    [Inject]
    private IJSRuntime JsRuntime { get; set; } = null!;

    [Inject]
    private NavigationManager NavigationManager { get; set; } = null!;

    [CascadingParameter(Name = "IdentityCookie")]
    public string IdentityCookie { get; set; } = null!;

    [CascadingParameter]
    public IModalService ModalService { get; set; } = null!;

    [Parameter]
    [EditorRequired]
    public string Ed2K { get; set; } = null!;

    [Parameter]
    [EditorRequired]
    public bool Single { get; set; }

    [Parameter]
    public string? Label { get; set; }

    protected override async Task OnParametersSetAsync()
    {
        _schemeHandlerInstalled = (await BrowserSettings.GetSettingsAsync(JsRuntime)).ExternalPlayerInstalled;
        var baseUri = new Uri(NavigationManager.BaseUri);

        _externalPlaybackUri = await GetExternalPlaylistUriAsync(Ed2K, Single, baseUri, IdentityCookie);
        await base.OnParametersSetAsync();
    }

    private async Task<string> GetExternalPlaylistUriAsync(string ed2K, bool single, Uri baseUri, string identityCookie)
    {
        var extPlayerScheme = (await BrowserSettings.GetSettingsAsync(JsRuntime)).ExternalPlayerScheme;

        IDictionary<string, object?> values = new ExpandoObject();
        values["ed2K"] = ed2K;
        values["single"] = single;
        values[IdentityConstants.ApplicationScheme] = identityCookie;
        var fileUri = LinkGenerator.GetUriByAction(nameof(FileServer.GetPlaylist), nameof(FileServer), values,
                          baseUri.Scheme, new HostString(baseUri.Authority), new PathString(baseUri.AbsolutePath)) ??
                      throw new ArgumentException("Failed to generate playlist uri");
        fileUri = Uri.EscapeDataString(fileUri);
        return $"{extPlayerScheme}{fileUri}";
    }

    private async Task OpenSchemeHandlerDownloadModalAsync()
    {
        await ModalService.Show<SchemeHandlerDownloadModal>().Result;
        _schemeHandlerInstalled = (await BrowserSettings.GetSettingsAsync(JsRuntime)).ExternalPlayerInstalled;
    }
}
