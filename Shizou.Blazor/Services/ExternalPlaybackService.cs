using System.Dynamic;
using System.Text.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.JSInterop;
using Shizou.Server.Controllers;

namespace Shizou.Blazor.Services;

public class ExternalPlaybackService
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly LinkGenerator _linkGenerator;
    private readonly IJSRuntime _jsRuntime;
    private string? _extPlayerScheme;

    public ExternalPlaybackService(IHttpContextAccessor httpContextAccessor, LinkGenerator linkGenerator, IJSRuntime jsRuntime)
    {
        _httpContextAccessor = httpContextAccessor;
        _linkGenerator = linkGenerator;
        _jsRuntime = jsRuntime;
    }

    public async Task<string> GetExternalPlaylistUriAsync(string ed2K, bool single)
    {
        if (_extPlayerScheme is null)
        {
            var playerSchemeRes = await _jsRuntime.InvokeAsync<JsonElement>("window.localStorage.getItem", LocalStorageKeys.ExternalPlayerScheme);
            _extPlayerScheme = playerSchemeRes is { ValueKind: JsonValueKind.String }
                ? playerSchemeRes.ToString()
                : LocalStorageKeys.ExternalPlayerSchemeDefault;
        }

        var identityCookie = _httpContextAccessor.HttpContext!.Request.Cookies[IdentityConstants.ApplicationScheme];
        IDictionary<string, object?> values = new ExpandoObject();
        values["ed2K"] = ed2K;
        values["single"] = single;
        values[IdentityConstants.ApplicationScheme] = identityCookie;
        var fileUri = _linkGenerator.GetUriByAction(_httpContextAccessor.HttpContext ?? throw new ArgumentNullException(), nameof(FileServer.GetPlaylist),
            nameof(FileServer), values) ?? throw new ArgumentException();
        fileUri = Uri.EscapeDataString(fileUri);
        return $"{_extPlayerScheme}{fileUri}";
    }
}
