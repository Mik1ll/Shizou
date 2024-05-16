using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Shizou.Data;

namespace Shizou.Blazor.Pages.Account;

[AllowAnonymous]
public class ChangePassword : PageModel
{
    public IdentityUser? AdminUser;
    private readonly SignInManager<IdentityUser> _signInManager;
    private readonly UserManager<IdentityUser> _userManager;

    public ChangePassword(SignInManager<IdentityUser> signInManager, UserManager<IdentityUser> userManager)
    {
        _signInManager = signInManager;
        _userManager = userManager;
        AdminUser = _userManager.Users.SingleOrDefault();
    }

    [BindProperty]
    public required InputModel Input { get; set; }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid) return Page();

        var returnUrl = Url.Content("~/");
        var password = Input.Password;
        var newPassword = Input.NewPassword;
        IdentityResult result;
        if (AdminUser is null)
        {
            AdminUser = new IdentityUser { UserName = Constants.IdentityUsername };
            result = await _userManager.CreateAsync(AdminUser, newPassword);
        }
        else
        {
            if (password is null)
            {
                ModelState.AddModelError($"{nameof(Input)}.{nameof(Input.Password)}", "Password is required");
                return Page();
            }

            if (await _userManager.CheckPasswordAsync(AdminUser, password).ConfigureAwait(false))
            {
                result = await _userManager
                    .ResetPasswordAsync(AdminUser, await _userManager.GeneratePasswordResetTokenAsync(AdminUser).ConfigureAwait(false), newPassword)
                    .ConfigureAwait(false);
            }
            else
            {
                ModelState.AddModelError($"{nameof(Input)}.{nameof(Input.Password)}", "Password is incorrect");
                return Page();
            }
        }

        if (!result.Succeeded)
        {
            ModelState.AddModelError(string.Empty, $"Something went wrong when creating account/changing password: {result}");
            return Page();
        }

        var signInResult = await _signInManager.PasswordSignInAsync(AdminUser, newPassword, true, false);
        if (!signInResult.Succeeded)
        {
            ModelState.AddModelError(string.Empty, $"Something went wrong when logging in after changing password: {signInResult}");
            return Page();
        }

        return LocalRedirect(returnUrl);
    }

    public record InputModel
    {
        [DataType(DataType.Password)]
        public string? Password { get; set; }

        [Required]
        [DataType(DataType.Password)]
        public required string NewPassword { get; set; }

        [Required]
        [DataType(DataType.Password)]
        [Compare(nameof(NewPassword))]
        public required string ConfirmNewPassword { get; set; }
    }
}
