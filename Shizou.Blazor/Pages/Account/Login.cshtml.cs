using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Shizou.Data;

namespace Shizou.Blazor.Pages.Account;

[AllowAnonymous]
public class Login : PageModel
{
    private readonly SignInManager<IdentityUser> _signInManager;

    public Login(SignInManager<IdentityUser> signInManager)
    {
        _signInManager = signInManager;
    }

    [BindProperty]
    public required InputModel Input { get; set; }

    public ActionResult OnGet(string returnUrl = "")
    {
        returnUrl = Url.Content("~/") + returnUrl.TrimStart('/');
        if (User.Identity?.IsAuthenticated ?? false)
            return LocalRedirect(returnUrl);
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(string returnUrl = "")
    {
        returnUrl = Url.Content("~/") + returnUrl.TrimStart('/');
        if (ModelState.IsValid)
        {
            var result = await _signInManager.PasswordSignInAsync(Constants.IdentityUsername, Input.Password, true, false);
            if (result.Succeeded)
                return LocalRedirect(returnUrl);
            ModelState.AddModelError("Input.Password", "Wrong password");
        }

        return Page();
    }

    public class InputModel
    {
        [Required]
        [DataType(DataType.Password)]
        public string Password { get; set; } = default!;
    }
}
