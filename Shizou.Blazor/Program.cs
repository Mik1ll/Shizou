using Blazored.Modal;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.Options;
using Scalar.AspNetCore;
using Serilog;
using Shizou.Blazor.Components;
using Shizou.Blazor.Components.Pages.Account;
using Shizou.Blazor.Services;
using Shizou.Data;
using Shizou.Server.Extensions;

Log.Logger = new LoggerConfiguration()
    .ConfigureSerilog(Constants.LogTemplate)
    .CreateBootstrapLogger();

Directory.CreateDirectory(FilePaths.ApplicationDataDir);

using var appMutex = new Mutex(false, Constants.AppLockName);
var hasHandle = false;
try
{
    try
    {
        hasHandle = appMutex.WaitOne(2000, false);
        if (!hasHandle)
        {
            Log.Logger.Error("Only one instance of Shizou may run per user");
            return;
        }
    }
    catch (AbandonedMutexException)
    {
        hasHandle = true;
    }

    var builder = WebApplication.CreateBuilder();

    builder.Services.AddRazorPages();
    builder.Services.AddRazorComponents()
        .AddInteractiveServerComponents();

    builder.Services.AddCascadingAuthenticationState();

    builder.Services.AddBlazoredModal();
    builder.Services.AddScoped<ToastService>();
    builder.Services.AddTransient<ExternalPlaybackService>();
    builder.Services.AddScoped<IdentityRedirectManager>();
    builder.Services.AddScoped<IdentityUserAccessor>();
    builder.Services.AddScoped<AuthenticationStateProvider, IdentityRevalidatingAuthenticationStateProvider>();

    builder.Services.Configure<StaticFileOptions>(options =>
    {
        options.ContentTypeProvider = new FileExtensionContentTypeProvider
        {
            Mappings =
            {
                [".ass"] = "text/x-ssa",
                [".ssa"] = "text/x-ssa",
                [".vtt"] = "text/vtt"
            }
        };
    });

    builder.AddShizouOptions()
        .AddShizouLogging(Constants.LogTemplate)
        .AddWorkerServices();

    builder.AddShizouServices()
        .AddShizouProcessors()
        .AddAniDbServices()
        .AddShizouApiServices();

    builder.Services.AddHsts(cfg => cfg.MaxAge = TimeSpan.FromSeconds(15_768_000));

    var app = builder.Build();

    app.UseForwardedHeaders(new ForwardedHeadersOptions
    {
        ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
    });

    if (!app.Environment.IsDevelopment())
    {
        app.UseExceptionHandler("/Error");
        app.UseHsts();
    }

    app.UseSwagger(opt => { opt.RouteTemplate = "/openapi/{documentName}.json"; });
    app.MapScalarApiReference();
    app.UseSwaggerUI(opt => { opt.SwaggerEndpoint("/openapi/v1.json", "V1"); });

    app.UseSerilogRequestLogging();

    app.UseHttpsRedirection();

    app.UseStaticFiles();
    app.UseRouting();

    app.UseIdentityCookieParameter();

    app.UseAuthentication();
    app.UseAuthorization();

    app.UseAntiforgery();

    app.UseSecurityHeaders();

    app.MapControllers().RequireAuthorization();

    app.MapHealthChecks("/healthz");

    app.MapRazorComponents<App>()
        .AddInteractiveServerRenderMode();

    app.MapAdditionalIdentityEndpoints();

    app.MigrateDatabase();

    app.Run();
}
catch (OptionsValidationException ex)
{
    foreach (var failure in ex.Failures) Log.Logger.Error("{Failure}", failure);
}
finally
{
    if (hasHandle)
        appMutex.ReleaseMutex();
}
