using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Shizou.Data;
using Swashbuckle.AspNetCore.Annotations;
using Swashbuckle.AspNetCore.Filters;

namespace Shizou.Server.Controllers;

[ApiController]
[Route($"{Constants.ApiPrefix}/[controller]")]
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
    [SwaggerResponse(StatusCodes.Status200OK)]
    [SwaggerResponseHeader(StatusCodes.Status200OK, "Set-Cookie", "string", "Sets the Identity cookie")]
    [SwaggerResponse(StatusCodes.Status400BadRequest)]
    [AllowAnonymous]
    public async Task<Results<Ok, BadRequest<string>>> Login([FromBody] [SwaggerRequestBody("Password", Required = true)] string password)
    {
        if (string.IsNullOrWhiteSpace(password)) return TypedResults.BadRequest("Password not supplied");
        var signInResult = await _signInManager.PasswordSignInAsync(Constants.IdentityUsername, password, true, false).ConfigureAwait(false);
        if (!signInResult.Succeeded) return TypedResults.BadRequest("Failed to log in");
        return TypedResults.Ok();
    }

    [HttpPost("SetPassword")]
    [SwaggerResponse(StatusCodes.Status200OK)]
    [SwaggerResponseHeader(StatusCodes.Status200OK, "Set-Cookie", "string", "Sets the Identity cookie")]
    [SwaggerResponse(StatusCodes.Status400BadRequest, type: typeof(ProblemDetails))]
    [SwaggerResponse(StatusCodes.Status500InternalServerError, type: typeof(ProblemDetails))]
    [AllowAnonymous]
    public async Task<Results<Ok, ProblemHttpResult>> ChangePassword([FromBody] PasswordModel passwordModel)
    {
        var password = passwordModel.Password;
        var newPassword = passwordModel.NewPassword;

        var user = _userManager.Users.SingleOrDefault();

        IdentityResult result;
        if (user is null)
        {
            user = new IdentityUser { UserName = Constants.IdentityUsername };
            result = await _userManager.CreateAsync(user, newPassword).ConfigureAwait(false);
        }
        else
        {
            if (await _userManager.CheckPasswordAsync(user, password).ConfigureAwait(false))
                result = await _userManager
                    .ResetPasswordAsync(user, await _userManager.GeneratePasswordResetTokenAsync(user).ConfigureAwait(false), newPassword)
                    .ConfigureAwait(false);
            else
                return TypedResults.Problem(title: "Password is incorrect", statusCode: StatusCodes.Status401Unauthorized);
        }

        if (!result.Succeeded)
            return TypedResults.Problem(title: $"Something went wrong when creating account/changing password: {result}",
                statusCode: StatusCodes.Status500InternalServerError);

        var signInResult = await _signInManager.PasswordSignInAsync(user, newPassword, true, false).ConfigureAwait(false);
        if (!signInResult.Succeeded)
            return TypedResults.Problem(title: $"Something went wrong when logging in after changing password: {signInResult}",
                statusCode: StatusCodes.Status500InternalServerError);
        return TypedResults.Ok();
    }

    public record PasswordModel([Required] string Password, [Required] string NewPassword);
}
