using System.Dynamic;
using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;
using Shizou.Data;
using Shizou.Server.Controllers;

namespace Shizou.Blazor.Services;

public class ExternalPlaybackService
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly LinkGenerator _linkGenerator;
    private readonly ProtectedLocalStorage _localStorage;
    private string? _extPlayerScheme;

    public ExternalPlaybackService(IHttpContextAccessor httpContextAccessor, LinkGenerator linkGenerator, ProtectedLocalStorage localStorage)
    {
        _httpContextAccessor = httpContextAccessor;
        _linkGenerator = linkGenerator;
        _localStorage = localStorage;
    }

    public async Task<string> GetExternalPlaylistUriAsync(string ed2K, bool single)
    {
        if (_extPlayerScheme is null)
        {
            var playerSchemeRes = await _localStorage.GetAsync<string>(LocalStorageKeys.ExternalPlayerScheme);
            _extPlayerScheme = playerSchemeRes.Value ?? LocalStorageKeys.ExternalPlayerSchemeDefault;
        }

        var identityCookie = _httpContextAccessor.HttpContext!.Request.Cookies[Constants.IdentityCookieName];
        IDictionary<string, object?> values = new ExpandoObject();
        values["ed2K"] = ed2K;
        values["single"] = single;
        values[Constants.IdentityCookieName] = identityCookie;
        var fileUri = _linkGenerator.GetUriByAction(_httpContextAccessor.HttpContext ?? throw new ArgumentNullException(), nameof(FileServer.GetPlaylist),
            nameof(FileServer), values) ?? throw new ArgumentException();
        return $"{_extPlayerScheme}:{fileUri}";
    }
}
