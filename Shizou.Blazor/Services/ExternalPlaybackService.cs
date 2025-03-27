using System.Dynamic;
using System.Text.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.JSInterop;
using Shizou.Server.Controllers;

namespace Shizou.Blazor.Services;

public class ExternalPlaybackService
{
    private readonly LinkGenerator _linkGenerator;
    private readonly IJSRuntime _jsRuntime;
    private string? _extPlayerScheme;

    public ExternalPlaybackService(LinkGenerator linkGenerator, IJSRuntime jsRuntime)
    {
        _linkGenerator = linkGenerator;
        _jsRuntime = jsRuntime;
    }

    public async Task<string> GetExternalPlaylistUriAsync(string ed2K, bool single, Uri baseUri, string identityCookie)
    {
        if (_extPlayerScheme is null)
        {
            var playerSchemeRes = await _jsRuntime.InvokeAsync<JsonElement>("window.localStorage.getItem", LocalStorageKeys.ExternalPlayerScheme);
            _extPlayerScheme = playerSchemeRes is { ValueKind: JsonValueKind.String }
                ? playerSchemeRes.ToString()
                : LocalStorageKeys.ExternalPlayerSchemeDefault;
        }

        IDictionary<string, object?> values = new ExpandoObject();
        values["ed2K"] = ed2K;
        values["single"] = single;
        values[IdentityConstants.ApplicationScheme] = identityCookie;
        var fileUri = _linkGenerator.GetUriByAction(nameof(FileServer.GetPlaylist), nameof(FileServer), values,
                          baseUri.Scheme, new HostString(baseUri.Authority), new PathString(baseUri.AbsolutePath)) ??
                      throw new ArgumentException("Failed to generate playlist uri");
        fileUri = Uri.EscapeDataString(fileUri);
        return $"{_extPlayerScheme}{fileUri}";
    }
}
