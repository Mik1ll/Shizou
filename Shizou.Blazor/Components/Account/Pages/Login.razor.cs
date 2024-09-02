﻿using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Identity;
using Shizou.Blazor.Services;
using Shizou.Data;

namespace Shizou.Blazor.Components.Account.Pages;

[AllowAnonymous]
public partial class Login : ComponentBase
{
    [SupplyParameterFromQuery]
    // ReSharper disable once UnusedAutoPropertyAccessor.Local
    private string? ReturnUrl { get; set; }

    [SupplyParameterFromForm]
    // ReSharper disable once AutoPropertyCanBeMadeGetOnly.Local
    private LoginModel Input { get; set; } = new();

    [Inject]
    private SignInManager<IdentityUser> SignInManager { get; set; } = default!;

    [Inject]
    private IdentityRedirectManager RedirectManager { get; set; } = default!;

    [CascadingParameter]
    private HttpContext HttpContext { get; set; } = default!;


    private async Task LoginUserAsync(EditContext editContext)
    {
        var messageStore = new ValidationMessageStore(editContext);
        
        var result = await SignInManager.PasswordSignInAsync(Constants.IdentityUsername, Input.Password, true, false);
        if (result.Succeeded)
            RedirectManager.RedirectTo(ReturnUrl);

        messageStore.Add(() => Input.Password, "Wrong password");
    }

    private sealed class LoginModel
    {
        [Required]
        [DataType(DataType.Password)]
        public string Password { get; set; } = "";
    }
}
