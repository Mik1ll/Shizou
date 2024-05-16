using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Shizou.Data;

namespace Shizou.Blazor.Pages.Account;

[AllowAnonymous]
public class ChangePassword : PageModel
{
    private readonly SignInManager<IdentityUser> _signInManager;
    private readonly UserManager<IdentityUser> _userManager;

    public ChangePassword(SignInManager<IdentityUser> signInManager, UserManager<IdentityUser> userManager)
    {
        _signInManager = signInManager;
        _userManager = userManager;
    }

    [BindProperty]
    public required InputModel Input { get; set; }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid) return Page();

        var returnUrl = Url.Content("~/");
        var password = Input.Password;
        var newPassword = Input.NewPassword;
        var user = await _userManager.Users.SingleOrDefaultAsync();
        IdentityResult result;
        if (user is null)
        {
            user = new IdentityUser { UserName = Constants.IdentityUsername };
            result = await _userManager.CreateAsync(user, newPassword);
        }
        else
        {
            if (await _userManager.CheckPasswordAsync(user, password).ConfigureAwait(false))
            {
                result = await _userManager
                    .ResetPasswordAsync(user, await _userManager.GeneratePasswordResetTokenAsync(user).ConfigureAwait(false), newPassword)
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
            ModelState.AddModelError("", $"Something went wrong when creating account/changing password: {result}");
            return Page();
        }

        var signInResult = await _signInManager.PasswordSignInAsync(user, newPassword, true, false);
        if (!signInResult.Succeeded)
        {
            ModelState.AddModelError("", $"Something went wrong when logging in after changing password: {signInResult}");
            return Page();
        }

        return LocalRedirect(returnUrl);
    }

    public record InputModel
    {
        [Required]
        [DataType(DataType.Password)]
        public required string Password { get; set; }

        [Required]
        [DataType(DataType.Password)]
        public required string NewPassword { get; set; }

        [Required]
        [DataType(DataType.Password)]
        [Compare(nameof(NewPassword))]
        public required string ConfirmNewPassword { get; set; }
    }
}
