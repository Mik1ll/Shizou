using Microsoft.AspNetCore.Components;

namespace Shizou.Blazor.Components.Account.Layout;

public partial class AccountLayout : LayoutComponentBase
{
    [Inject]
    private NavigationManager NavigationManager { get; set; } = default!;

    [CascadingParameter]
    private HttpContext? HttpContext { get; set; }

    protected override void OnParametersSet()
    {
        if (HttpContext is null)
            // If this code runs, we're currently rendering in interactive mode, so there is no HttpContext.
            // The identity pages need to set cookies, so they require an HttpContext. To achieve this we
            // must transition back from interactive mode to a server-rendered page.
            NavigationManager.Refresh(true);
    }
}
