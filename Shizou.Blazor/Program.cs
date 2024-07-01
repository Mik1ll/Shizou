using Blazored.Modal;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.StaticFiles;
using Serilog;
using Shizou.Blazor.Components;
using Shizou.Blazor.Services;
using Shizou.Data;
using Shizou.Server.Extensions;
using WebEssentials.AspNetCore.Pwa;

Log.Logger = new LoggerConfiguration()
    .ConfigureSerilog()
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

    builder.Services.AddProgressiveWebApp(new PwaOptions
    {
        Strategy = ServiceWorkerStrategy.Minimal
    });

    builder.Services.AddCascadingAuthenticationState();

    builder.Services.AddBlazoredModal();
    builder.Services.AddScoped<ToastService>();
    builder.Services.AddTransient<ExternalPlaybackService>();

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
        .AddShizouLogging()
        .AddWorkerServices();

    builder.Services
        .AddShizouServices()
        .AddShizouProcessors()
        .AddAniDbServices()
        .AddShizouApiServices();

    builder.Services.AddHsts(cfg => cfg.MaxAge = TimeSpan.FromSeconds(15_768_000));

    var app = builder.Build();

    app.UseForwardedHeaders(new ForwardedHeadersOptions
    {
        ForwardedHeaders = ForwardedHeaders.All
    });

    if (!app.Environment.IsDevelopment())
    {
        app.UseExceptionHandler("/Error");
        app.UseHsts();
    }


    app.UseSwagger();
    app.UseSwaggerUI();

    app.UseSerilogRequestLogging();

    app.UseHttpsRedirection();

    app.UseStaticFiles();
    app.UseRouting();

    app.UseIdentityCookieParameter();

    app.UseAuthentication();
    app.UseAuthorization();

    app.UseAntiforgery();

    app.UseSecurityHeaders();

    app.MapControllers().Finally(endpointBuilder =>
    {
        // PWA manifest/service worker is locked behind auth without this
        if (typeof(PwaController).FullName is { } fn && (endpointBuilder.DisplayName?.StartsWith(fn) ?? false))
            endpointBuilder.Metadata.Add(new AllowAnonymousAttribute());
    });

    app.MapRazorPages();
    app.MapRazorComponents<App>()
        .AddInteractiveServerRenderMode();

    app.MigrateDatabase();

    app.Run();
}
finally
{
    if (hasHandle)
        appMutex.ReleaseMutex();
}
