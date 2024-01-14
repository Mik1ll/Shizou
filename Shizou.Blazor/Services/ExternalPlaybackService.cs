using System.Dynamic;
using Shizou.Data;
using Shizou.Server.Controllers;

namespace Shizou.Blazor.Services;

public class ExternalPlaybackService
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly LinkGenerator _linkGenerator;

    public ExternalPlaybackService(IHttpContextAccessor httpContextAccessor, LinkGenerator linkGenerator)
    {
        _httpContextAccessor = httpContextAccessor;
        _linkGenerator = linkGenerator;
    }

    public string GetExternalPlaylistUri(string ed2K, bool single)
    {
        var identityCookie = _httpContextAccessor.HttpContext!.Request.Cookies[Constants.IdentityCookieName];
        IDictionary<string, object?> values = new ExpandoObject();
        values["ed2K"] = ed2K;
        values["single"] = single;
        values[Constants.IdentityCookieName] = identityCookie;
        var fileUri = _linkGenerator.GetUriByAction(_httpContextAccessor.HttpContext ?? throw new ArgumentNullException(), nameof(FileServer.GetPlaylist),
            nameof(FileServer), values) ?? throw new ArgumentException();
        return $"shizou:{fileUri}";
    }
}
