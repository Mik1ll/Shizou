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

    public string GetExternalPlaylistUri(int localFileId, bool single)
    {
        var identityCookie = _httpContextAccessor.HttpContext!.Request.Cookies[Constants.IdentityCookieName];
        IDictionary<string, object?> values = new ExpandoObject();
        values["localFileId"] = $"{localFileId}.m3u8";
        values["single"] = single;
        values[Constants.IdentityCookieName] = identityCookie;
        var fileUri = _linkGenerator.GetUriByAction(_httpContextAccessor.HttpContext ?? throw new ArgumentNullException(), nameof(FileServer.GetWithPlaylist),
            nameof(FileServer), values) ?? throw new ArgumentException();
        return $"shizou:{fileUri}";
    }
}
