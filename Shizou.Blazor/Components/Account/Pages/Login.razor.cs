using System.ComponentModel.DataAnnotations;
using BlazorApp1.Components.Account;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Identity;
using Shizou.Data;

namespace Shizou.Blazor.Components.Account.Pages;

public partial class Login : ComponentBase
{
    [Inject]
    private SignInManager<IdentityUser> SignInManager { get; set; } = default!;

    [Inject]
    private IdentityRedirectManager RedirectManager { get; set; } = default!;


    [CascadingParameter]
    private HttpContext HttpContext { get; set; } = default!;

    [SupplyParameterFromQuery]
    private string? ReturnUrl { get; }

    [SupplyParameterFromForm]
    private LoginModel Input { get; } = new(string.Empty);

    private string? _errorMessage;


    private async Task LoginUserAsync()
    {
        var result = await SignInManager.PasswordSignInAsync(Constants.IdentityUsername, Input.Password, true, false);
        if (result.Succeeded)
            RedirectManager.RedirectTo(ReturnUrl);
        _errorMessage = "Wrong password";
    }

    public record LoginModel(
        [Required]
        [DataType(DataType.Password)]
        string Password);
}
