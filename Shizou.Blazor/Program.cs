using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Serilog;
using Shizou.Blazor;
using Shizou.Data;
using Shizou.Data.Database;
using Shizou.Server;
using Shizou.Server.Extensions;

var logTemplate = "{Timestamp:HH:mm:ss} {Level:u3} | {SourceContext} {Message:lj}{NewLine:1}{Exception:1}";
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console(outputTemplate: logTemplate)
    .CreateBootstrapLogger();

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();

builder.AddShizouOptions()
    .AddShizouLogging(logTemplate)
    .AddShizouServices()
    .AddAniDbServices()
    .AddShizouProcessors()
    .AddShizouApiServices();

builder.Services.AddHostedService<StartupService>();

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
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(FilePaths.ImagesDir),
    RequestPath = WebPaths.ImagesDir
});

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers().RequireAuthorization();

app.MapBlazorHub();
app.MapFallbackToPage("/_Host");

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;

    var context = services.GetRequiredService<ShizouContext>();
    context.Database.Migrate();
}

app.Run();