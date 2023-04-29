using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace ShizouBlazor.Areas.Identity.Pages.Account;

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
    public InputModel Input { get; set; }

    public string ReturnUrl { get; set; }

    public void OnGet()
    {
        ReturnUrl = Url.Content("~/");
    }

    public async Task<IActionResult> OnPostAsync()
    {
        ReturnUrl = Url.Content("~/");
        if (!IsLoopBackAddress(HttpContext)) return Forbid("Must be on localhost to change password");
        if (ModelState.IsValid)
        {
            var identity = await _userManager.Users.FirstOrDefaultAsync(u => u.UserName == "Admin");
            IdentityResult result;
            if (identity is null)
            {
                identity = new IdentityUser { UserName = "Admin" };
                result = await _userManager.CreateAsync(identity, Input.Password);
            }
            else
            {
                result = await _userManager.ResetPasswordAsync(identity, await _userManager.GeneratePasswordResetTokenAsync(identity), Input.Password);
            }
            if (result.Succeeded)
            {
                await _signInManager.SignInAsync(identity, true);
                return LocalRedirect(ReturnUrl);
            }
        }
        return Page();
    }

    public static bool IsLoopBackAddress(HttpContext context)
    {
        string? ipString;
        if (string.IsNullOrEmpty(ipString = context.Request.Headers["X-Forwarded-For"].ToString()))
            ipString = context.Connection.RemoteIpAddress?.ToString();

        // if unknown, assume not local
        if (string.IsNullOrEmpty(ipString))
            return false;

        // check if localhost
        if (ipString is "127.0.0.1" or "::1")
            return true;

        // compare with local address
        return ipString == context.Connection.LocalIpAddress?.ToString();
    }

    public class InputModel
    {
        [Required]
        [DataType(DataType.Password)]
        public string Password { get; set; }

        [Required]
        [DataType(DataType.Password)]
        [Compare(nameof(Password))]
        public string ConfirmPassword { get; set; }
    }
}
