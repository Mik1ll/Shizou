using Blazored.Modal;
using Microsoft.Extensions.FileProviders;
using Serilog;
using Shizou.Blazor;
using Shizou.Data;
using Shizou.Server;
using Shizou.Server.Extensions;

Log.Logger = new LoggerConfiguration()
    .ConfigureSerilog()
    .CreateBootstrapLogger();

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();
builder.Services.AddBlazoredModal();

builder.AddShizouOptions()
    .AddShizouLogging();

builder.Services
    .AddShizouServices()
    .AddShizouProcessors()
    .AddAniDbServices()
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

app.MigrateDatabase();
app.PopulateOptions();

app.Run();
