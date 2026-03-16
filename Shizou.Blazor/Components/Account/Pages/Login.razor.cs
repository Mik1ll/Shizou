using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Identity;
using Shizou.Data;

namespace Shizou.Blazor.Components.Account.Pages;

[AllowAnonymous]
public partial class Login : ComponentBase
{
    [SupplyParameterFromQuery]
    private string? ReturnUrl { get; set; }

    [SupplyParameterFromForm]
    private InputModel? Input { get; set; }

    [Inject]
    private SignInManager<IdentityUser> SignInManager { get; set; } = null!;

    [Inject]
    private IdentityRedirectManager RedirectManager { get; set; } = null!;

    [CascadingParameter]
    private HttpContext HttpContext { get; set; } = null!;

    protected override async Task OnInitializedAsync()
    {
        Input ??= new InputModel();
        if (HttpMethods.IsGet(HttpContext.Request.Method))
            // Clear the existing external cookie to ensure a clean login process
            await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme);
    }

    private async Task LoginUserAsync(EditContext editContext)
    {
        var messageStore = new ValidationMessageStore(editContext);

        var result = await SignInManager.PasswordSignInAsync(Constants.IdentityUsername, Input!.Password, true, false);
        if (result.Succeeded)
            RedirectManager.RedirectTo(ReturnUrl);
        else
            messageStore.Add(() => Input.Password, "Wrong password");
    }

    private sealed class InputModel
    {
        [Required]
        [DataType(DataType.Password)]
        public string Password { get; set; } = "";
    }
}
