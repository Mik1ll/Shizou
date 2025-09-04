using System.Dynamic;
using Blazored.Modal.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Identity;
using Shizou.Server.Controllers;

namespace Shizou.Blazor.Components.Shared;

public partial class DownloadFileButton : ComponentWithExtraClasses
{
    private string _fileDownloadUrl = null!;

    [Inject]
    private NavigationManager NavigationManager { get; set; } = null!;

    [Inject]
    private LinkGenerator LinkGenerator { get; set; } = null!;

    [CascadingParameter]
    public IModalService ModalService { get; set; } = null!;

    [CascadingParameter(Name = "IdentityCookie")]
    public string IdentityCookie { get; set; } = null!;

    [Parameter]
    [EditorRequired]
    public string Ed2K { get; set; } = null!;

    [Parameter]
    public string? Label { get; set; }

    protected override void OnParametersSet()
    {
        var baseUri = new Uri(NavigationManager.BaseUri);
        IDictionary<string, object?> values = new ExpandoObject();
        values["ed2K"] = Ed2K;
        values[IdentityConstants.ApplicationScheme] = IdentityCookie;
        _fileDownloadUrl = LinkGenerator.GetUriByAction(nameof(FileServer.Get), nameof(FileServer), values, baseUri.Scheme, new HostString(baseUri.Authority),
            new PathString(baseUri.AbsolutePath)) ?? throw new ArgumentException("Failed to generate file download uri");
        base.OnParametersSet();
    }
}
