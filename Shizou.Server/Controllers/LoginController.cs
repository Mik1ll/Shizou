using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace Shizou.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class LoginController : ControllerBase
{
    private readonly SignInManager<IdentityUser> _signInManager;

    public LoginController(SignInManager<IdentityUser> signInManager)
    {
        _signInManager = signInManager;
    }

    [HttpPost]
    [AllowAnonymous]
    public async Task<ActionResult> Login([FromBody] string? password)
    {
        if (password is null) return BadRequest("Password not supplied");
        var result = await _signInManager.PasswordSignInAsync("Admin", password, true, false);
        if (result.Succeeded)
            return LocalRedirect("~/");
        return Forbid("Wrong password");
    }
}
