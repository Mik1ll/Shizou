using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Swashbuckle.AspNetCore.Annotations;

namespace Shizou.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AccountController : ControllerBase
{
    private readonly SignInManager<IdentityUser> _signInManager;
    private readonly UserManager<IdentityUser> _userManager;

    public AccountController(SignInManager<IdentityUser> signInManager, UserManager<IdentityUser> userManager)
    {
        _signInManager = signInManager;
        _userManager = userManager;
    }

    [HttpPost("Login")]
    [SwaggerResponse(StatusCodes.Status400BadRequest, "If the password is wrong")]
    [SwaggerResponse(StatusCodes.Status200OK)]
    [Consumes("application/json")]
    [AllowAnonymous]
    public async Task<ActionResult> Login([FromBody] string? password)
    {
        if (password is null) return BadRequest("Password not supplied");
        var signInResult = await _signInManager.PasswordSignInAsync("Admin", password, true, false);
        var token = HttpContext.Response.GetTypedHeaders().SetCookie.FirstOrDefault(c => c.Name == ".AspNetCore.Identity.Application");
        if (signInResult.Succeeded && token is not null) return Ok(token.Value.Value);

        return BadRequest();
    }

    [HttpPost("SetPassword")]
    [SwaggerResponse(StatusCodes.Status400BadRequest)]
    [SwaggerResponse(StatusCodes.Status500InternalServerError)]
    [SwaggerResponse(StatusCodes.Status200OK)]
    [Consumes("application/json")]
    [AllowAnonymous]
    public async Task<ActionResult> SetPassword([FromBody] string? password)
    {
        if (password is null) return BadRequest("Password not supplied");
        if (!IsLoopBackAddress(HttpContext)) return BadRequest("Must be local to change password");
        var identity = await _userManager.Users.FirstOrDefaultAsync(u => u.UserName == "Admin");
        IdentityResult result;
        if (identity is null)
        {
            identity = new IdentityUser { UserName = "Admin" };
            result = await _userManager.CreateAsync(identity, password);
        }
        else
        {
            result = await _userManager.ResetPasswordAsync(identity, await _userManager.GeneratePasswordResetTokenAsync(identity), password);
        }

        if (result.Succeeded)
        {
            var signInResult = await _signInManager.PasswordSignInAsync("Admin", password, true, false);
            var token = HttpContext.Response.GetTypedHeaders().SetCookie.FirstOrDefault(c => c.Name == ".AspNetCore.Identity.Application");
            if (signInResult.Succeeded && token is not null) return Ok(token.Value.Value);

            return StatusCode(StatusCodes.Status500InternalServerError, "Something went wrong when logging in after changing password");
        }

        return StatusCode(StatusCodes.Status500InternalServerError, "Something went wrong when creating account/changing password");
    }

    public static bool IsLoopBackAddress(HttpContext context)
    {
        return (context.Connection.RemoteIpAddress?.ToString(), context.Connection.LocalIpAddress?.ToString()) is ("127.0.0.1", "127.0.0.1") or ("::1", "::1");
    }
}