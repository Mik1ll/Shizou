using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Shizou.Data;
using Swashbuckle.AspNetCore.Annotations;

namespace Shizou.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class Account : ControllerBase
{
    private readonly SignInManager<IdentityUser> _signInManager;
    private readonly UserManager<IdentityUser> _userManager;

    public Account(SignInManager<IdentityUser> signInManager, UserManager<IdentityUser> userManager)
    {
        _signInManager = signInManager;
        _userManager = userManager;
    }

    [HttpPost("Login")]
    [SwaggerResponse(StatusCodes.Status400BadRequest, "If the password is wrong")]
    [SwaggerResponse(StatusCodes.Status200OK)]
    [Consumes("application/json")]
    [AllowAnonymous]
    public async Task<Results<Ok<string>, BadRequest<string>>> Login([FromBody] string? password)
    {
        if (password is null) return TypedResults.BadRequest("Password not supplied");
        var signInResult = await _signInManager.PasswordSignInAsync("Admin", password, true, false).ConfigureAwait(false);
        var token = HttpContext.Response.GetTypedHeaders().SetCookie.FirstOrDefault(c => c.Name == Constants.IdentityCookieName);
        if (!signInResult.Succeeded || token is null) return TypedResults.BadRequest("Failed to log in");
        return TypedResults.Ok(token.Value.Value);
    }

    [HttpPost("SetPassword")]
    [SwaggerResponse(StatusCodes.Status200OK)]
    [SwaggerResponse(StatusCodes.Status400BadRequest)]
    [SwaggerResponse(StatusCodes.Status500InternalServerError, type: typeof(ProblemDetails))]
    [Consumes("application/json")]
    [AllowAnonymous]
    public async Task<Results<Ok<string>, BadRequest<string>, ProblemHttpResult>> SetPassword([FromBody] string? password)
    {
        if (password is null) return TypedResults.BadRequest("Password not supplied");
        if (HttpContext.Connection.RemoteIpAddress is null || !IPAddress.IsLoopback(HttpContext.Connection.RemoteIpAddress))
            return TypedResults.BadRequest("Must be local to change password");
        var identity = await _userManager.Users.FirstOrDefaultAsync(u => u.UserName == "Admin").ConfigureAwait(false);
        IdentityResult result;
        if (identity is null)
        {
            identity = new IdentityUser { UserName = "Admin" };
            result = await _userManager.CreateAsync(identity, password).ConfigureAwait(false);
        }
        else
        {
            result = await _userManager
                .ResetPasswordAsync(identity, await _userManager.GeneratePasswordResetTokenAsync(identity).ConfigureAwait(false), password)
                .ConfigureAwait(false);
        }

        if (!result.Succeeded)
            return TypedResults.Problem(title: "Something went wrong when creating account/changing password",
                statusCode: StatusCodes.Status500InternalServerError);

        var signInResult = await _signInManager.PasswordSignInAsync("Admin", password, true, false).ConfigureAwait(false);
        var token = HttpContext.Response.GetTypedHeaders().SetCookie.FirstOrDefault(c => c.Name == Constants.IdentityCookieName);
        if (!signInResult.Succeeded || token is null)
            return TypedResults.Problem(title: "Something went wrong when logging in after changing password",
                statusCode: StatusCodes.Status500InternalServerError);
        return TypedResults.Ok(token.Value.Value);
    }
}
