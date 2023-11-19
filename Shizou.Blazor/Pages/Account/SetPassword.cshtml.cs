using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Shizou.Blazor.Pages.Account;

public class SetPassword : PageModel
{
    private readonly SignInManager<IdentityUser> _signInManager;
    private readonly UserManager<IdentityUser> _userManager;

    public SetPassword(SignInManager<IdentityUser> signInManager, UserManager<IdentityUser> userManager)
    {
        _signInManager = signInManager;
        _userManager = userManager;
    }

    [BindProperty]
    public required InputModel Input { get; set; }

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var returnUrl = Url.Content("~/");
        if (ModelState.IsValid)
        {
            var identity = await _userManager.Users.FirstOrDefaultAsync(u => u.UserName == "Admin");
            IdentityResult result;
            if (identity is null)
            {
                identity = new IdentityUser { UserName = "Admin" };
                result = await _userManager.CreateAsync(identity, Input.Password!);
            }
            else
            {
                result = await _userManager.ResetPasswordAsync(identity, await _userManager.GeneratePasswordResetTokenAsync(identity), Input.Password!);
            }

            if (result.Succeeded)
            {
                await _signInManager.SignInAsync(identity, true);
                return LocalRedirect(returnUrl);
            }
        }

        return Page();
    }

    [SuppressMessage("ReSharper", "UnusedMember.Global")]
    public class InputModel
    {
        [Required]
        [DataType(DataType.Password)]
        public string? Password { get; set; }

        [Required]
        [DataType(DataType.Password)]
        [Compare(nameof(Password))]
        public string? ConfirmPassword { get; set; }
    }
}
