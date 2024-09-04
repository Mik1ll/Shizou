using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Identity;
using Shizou.Data;

namespace Shizou.Blazor.Components.Pages.Account;

[AllowAnonymous]
public partial class ChangePassword : ComponentBase
{
    private IdentityUser? _adminUser;

    [SupplyParameterFromForm]
    // ReSharper disable once AutoPropertyCanBeMadeGetOnly.Local
    private InputModel Input { get; set; } = new();

    [Inject]
    private SignInManager<IdentityUser> SignInManager { get; set; } = default!;

    [Inject]
    private NavigationManager NavigationManager { get; set; } = default!;


    protected override void OnInitialized()
    {
        _adminUser = SignInManager.UserManager.Users.SingleOrDefault();
    }

    private async Task ChangePasswordAsync(EditContext editContext)
    {
        var messageStore = new ValidationMessageStore(editContext);

        IdentityResult result;

        if (_adminUser is null)
        {
            _adminUser = new IdentityUser { UserName = Constants.IdentityUsername };
            result = await SignInManager.UserManager.CreateAsync(_adminUser, Input.NewPassword);
        }
        else
        {
            if (await SignInManager.UserManager.CheckPasswordAsync(_adminUser, Input.Password).ConfigureAwait(false))
            {
                result = await SignInManager.UserManager
                    .ResetPasswordAsync(_adminUser, await SignInManager.UserManager.GeneratePasswordResetTokenAsync(_adminUser).ConfigureAwait(false),
                        Input.NewPassword)
                    .ConfigureAwait(false);
            }
            else
            {
                messageStore.Add(() => Input.Password, "Password is incorrect");
                return;
            }
        }

        if (!result.Succeeded)
        {
            messageStore.Add(() => Input, $"Something went wrong when creating account/changing password: {result}");
            return;
        }

        var signInResult = await SignInManager.PasswordSignInAsync(_adminUser, Input.NewPassword, true, false);
        if (!signInResult.Succeeded)
        {
            messageStore.Add(() => Input, $"Something went wrong when logging in after changing password: {signInResult}");
            return;
        }

        NavigationManager.NavigateTo("./");
    }

    private sealed class InputModel
    {
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        [Required]
        [DataType(DataType.Password)]
        public string NewPassword { get; set; } = string.Empty;

        [Required]
        [DataType(DataType.Password)]
        [Compare(nameof(NewPassword))]
        public string ConfirmNewPassword { get; set; } = string.Empty;
    }
}
