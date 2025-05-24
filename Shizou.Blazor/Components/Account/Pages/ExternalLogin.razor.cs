using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Identity;

namespace Shizou.Blazor.Components.Account.Pages;

[AllowAnonymous]
public partial class ExternalLogin
{
    public const string LoginCallbackAction = "LoginCallback";
    private ExternalLoginInfo _externalLoginInfo = null!;

    [SupplyParameterFromQuery]
    private string? RemoteError { get; set; }

    [SupplyParameterFromQuery]
    private string? ReturnUrl { get; set; }

    [SupplyParameterFromQuery]
    private string? Action { get; set; }

    private string? ProviderDisplayName => _externalLoginInfo.ProviderDisplayName;

    [Inject]
    private SignInManager<IdentityUser> SignInManager { get; set; } = null!;

    [Inject]
    private IdentityRedirectManager RedirectManager { get; set; } = null!;

    [Inject]
    private ILogger<ExternalLogin> Logger { get; set; } = null!;

    [CascadingParameter]
    private HttpContext HttpContext { get; set; } = null!;

    protected override async Task OnInitializedAsync()
    {
        if (RemoteError is not null) RedirectManager.RedirectToWithStatus("Account/Login", $"Error from external provider: {RemoteError}", HttpContext);

        var info = await SignInManager.GetExternalLoginInfoAsync();
        if (info is null) RedirectManager.RedirectToWithStatus("Account/Login", "Error loading external login information.", HttpContext);

        _externalLoginInfo = info;

        if (HttpMethods.IsGet(HttpContext.Request.Method))
        {
            if (Action == LoginCallbackAction)
            {
                await OnLoginCallbackAsync();
                return;
            }

            // We should only reach this page via the login callback, so redirect back to
            // the login page if we get here some other way.
            RedirectManager.RedirectTo("Account/Login");
        }
    }

    private async Task OnLoginCallbackAsync()
    {
        // Sign in the user with this external login provider if the user already has a login.
        var result = await SignInManager.ExternalLoginSignInAsync(
            _externalLoginInfo.LoginProvider,
            _externalLoginInfo.ProviderKey,
            false,
            true);

        if (result.Succeeded)
        {
            Logger.LogInformation(
                "{Name} logged in with {LoginProvider} provider.",
                _externalLoginInfo.Principal.Identity?.Name,
                _externalLoginInfo.LoginProvider);
            RedirectManager.RedirectTo(ReturnUrl);
        }
        else if (result.IsLockedOut)
        {
            RedirectManager.RedirectTo("Account/Lockout");
        }
    }
}
