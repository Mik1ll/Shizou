using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Shizou.Blazor.Pages.Account;

public class Login : PageModel
{
    private readonly SignInManager<IdentityUser> _signInManager;

    public Login(SignInManager<IdentityUser> signInManager)
    {
        _signInManager = signInManager;
    }

    [BindProperty] public required InputModel Input { get; set; }

    public string ReturnUrl { get; set; } = "";

    public ActionResult OnGet(string returnUrl = "")
    {
        ReturnUrl = Url.Content("~/") + returnUrl.TrimStart('/');
        if (User.Identity?.IsAuthenticated ?? false)
            return LocalRedirect(ReturnUrl);
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        ReturnUrl = Url.Content("~/") + ReturnUrl.TrimStart('/');
        if (ModelState.IsValid)
        {
            var result = await _signInManager.PasswordSignInAsync("Admin", Input.Password!, true, false);
            if (result.Succeeded)
                return LocalRedirect(ReturnUrl);
            ModelState.AddModelError("Input.Password", "Wrong password");
        }

        return Page();
    }

    public class InputModel
    {
        [Required]
        [DataType(DataType.Password)]
        public string? Password { get; set; }
    }
}