using Blazored.Modal;
using Microsoft.AspNetCore.StaticFiles;
using Serilog;
using Shizou.Blazor.Features;
using Shizou.Blazor.Services;
using Shizou.Data;
using Shizou.Server.Extensions;

Log.Logger = new LoggerConfiguration()
    .ConfigureSerilog()
    .CreateBootstrapLogger();

Directory.CreateDirectory(FilePaths.ApplicationDataDir);

var builder = WebApplication.CreateBuilder();

builder.Services.AddRazorPages();
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

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

app.MapControllers();
app.Map($"{Constants.ApiPrefix}/{{**slug}}", (HttpContext ctx) => { ctx.Response.StatusCode = StatusCodes.Status404NotFound; });

app.MapRazorPages();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.MigrateDatabase();
app.PopulateOptions();

app.Run();
