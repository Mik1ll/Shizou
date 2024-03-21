using Blazored.Modal;
using Microsoft.AspNetCore.Authorization;
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

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
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

app.MapControllers().Finally(endpointBuilder =>
{
    // PWA manifest/service worker is locked behind auth without this
    if (typeof(PwaController).FullName is { } fn && (endpointBuilder.DisplayName?.StartsWith(fn) ?? false))
        endpointBuilder.Metadata.Add(new AllowAnonymousAttribute());
});

app.MapRazorPages();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.PopulateOptions();
app.MigrateDatabase();

app.Run();
