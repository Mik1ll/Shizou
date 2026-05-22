using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.OpenApi;
using Shizou.Data;
using Swashbuckle.AspNetCore.Annotations;
using Swashbuckle.AspNetCore.Filters;

namespace Shizou.Server.Controllers;

[ApiController]
[Route($"{Constants.ApiPrefix}/[controller]")]
public class Account(SignInManager<IdentityUser> signInManager, UserManager<IdentityUser> userManager) : ControllerBase
{
    [HttpPost("Login")]
    [SwaggerResponse(StatusCodes.Status200OK)]
    [SwaggerResponseHeader(StatusCodes.Status200OK, "Set-Cookie", JsonSchemaType.String, "Sets the Identity cookie")]
    [SwaggerResponse(StatusCodes.Status401Unauthorized)]
    [AllowAnonymous]
    public async Task<Results<Ok, UnauthorizedHttpResult>> Login([FromBody] [SwaggerRequestBody("Password", Required = true)] string password)
    {
        var signInResult = await signInManager.PasswordSignInAsync(Constants.IdentityUsername, password, true, false).ConfigureAwait(false);
        if (!signInResult.Succeeded) return TypedResults.Unauthorized();
        return TypedResults.Ok();
    }

    [HttpPost("SetPassword")]
    [SwaggerResponse(StatusCodes.Status200OK)]
    [SwaggerResponseHeader(StatusCodes.Status200OK, "Set-Cookie", JsonSchemaType.String, "Sets the Identity cookie")]
    [SwaggerResponse(StatusCodes.Status400BadRequest)]
    [SwaggerResponse(StatusCodes.Status500InternalServerError)]
    [AllowAnonymous]
    public async Task<Results<Ok, ProblemHttpResult>> ChangePassword([FromBody] PasswordModel passwordModel)
    {
        var password = passwordModel.Password;
        var newPassword = passwordModel.NewPassword;

        var user = userManager.Users.SingleOrDefault();

        IdentityResult result;
        if (user is null)
        {
            user = new IdentityUser { UserName = Constants.IdentityUsername };
            result = await userManager.CreateAsync(user, newPassword).ConfigureAwait(false);
        }
        else
        {
            if (await userManager.CheckPasswordAsync(user, password).ConfigureAwait(false))
                result = await userManager
                    .ResetPasswordAsync(user, await userManager.GeneratePasswordResetTokenAsync(user).ConfigureAwait(false), newPassword)
                    .ConfigureAwait(false);
            else
                return TypedResults.Problem(title: "Password is incorrect", statusCode: StatusCodes.Status401Unauthorized);
        }

        if (!result.Succeeded)
            return TypedResults.Problem(title: $"Something went wrong when creating account/changing password: {result}",
                statusCode: StatusCodes.Status500InternalServerError);

        var signInResult = await signInManager.PasswordSignInAsync(user, newPassword, true, false).ConfigureAwait(false);
        if (!signInResult.Succeeded)
            return TypedResults.Problem(title: $"Something went wrong when logging in after changing password: {signInResult}",
                statusCode: StatusCodes.Status500InternalServerError);
        return TypedResults.Ok();
    }

    public record PasswordModel([Required] string Password, [Required] string NewPassword);
}
