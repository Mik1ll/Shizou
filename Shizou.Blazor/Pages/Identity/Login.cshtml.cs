using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Shizou.Blazor.Pages.Identity;

public class Login : PageModel
{
    private readonly SignInManager<IdentityUser> _signInManager;

    public Login(SignInManager<IdentityUser> signInManager)
    {
        _signInManager = signInManager;
    }

    [BindProperty]
    public required InputModel Input { get; set; }

    [BindProperty]
    public string ReturnUrl { get; set; } = "";

    public void OnGet(string returnUrl = "")
    {
        ReturnUrl = returnUrl;
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var returnUrl = Url.Content("~/") + ReturnUrl;
        if (ModelState.IsValid)
        {
            var result = await _signInManager.PasswordSignInAsync("Admin", Input.Password!, true, false);
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
        public string? Password { get; set; }
    }
}
