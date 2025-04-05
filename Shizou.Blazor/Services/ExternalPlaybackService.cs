using System.Dynamic;
using Microsoft.AspNetCore.Identity;
using Microsoft.JSInterop;
using Shizou.Server.Controllers;

namespace Shizou.Blazor.Services;

public class ExternalPlaybackService
{
    private readonly LinkGenerator _linkGenerator;
    private readonly IJSRuntime _jsRuntime;

    public ExternalPlaybackService(LinkGenerator linkGenerator, IJSRuntime jsRuntime)
    {
        _linkGenerator = linkGenerator;
        _jsRuntime = jsRuntime;
    }

    public async Task<string> GetExternalPlaylistUriAsync(string ed2K, bool single, Uri baseUri, string identityCookie)
    {
        var extPlayerScheme = (await BrowserSettings.GetSettingsAsync(_jsRuntime)).ExternalPlayerScheme;

        IDictionary<string, object?> values = new ExpandoObject();
        values["ed2K"] = ed2K;
        values["single"] = single;
        values[IdentityConstants.ApplicationScheme] = identityCookie;
        var fileUri = _linkGenerator.GetUriByAction(nameof(FileServer.GetPlaylist), nameof(FileServer), values,
                          baseUri.Scheme, new HostString(baseUri.Authority), new PathString(baseUri.AbsolutePath)) ??
                      throw new ArgumentException("Failed to generate playlist uri");
        fileUri = Uri.EscapeDataString(fileUri);
        return $"{extPlayerScheme}{fileUri}";
    }
}
