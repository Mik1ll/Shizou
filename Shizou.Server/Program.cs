using System;
using System.IO;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using Shizou.Data;
using Shizou.Server;
using Shizou.Server.Extensions;

Log.Logger = new LoggerConfiguration()
    .ConfigureSerilog()
    .CreateBootstrapLogger();

try
{
    Directory.CreateDirectory(FilePaths.ApplicationDataDir);

    var builder = WebApplication.CreateBuilder();

    builder.AddShizouOptions()
        .AddShizouLogging();

    builder.Services
        .AddShizouServices()
        .AddShizouProcessors()
        .AddAniDbServices()
        .AddShizouApiServices();

    builder.Services.AddCors(options =>
    {
        options.AddPolicy("HttpScheme",
            policy => { policy.WithOrigins("http://localhost"); });
    });

    builder.Services.AddHostedService<StartupService>();

    var app = builder.Build();

    if (!app.Environment.IsDevelopment())
        app.UseHsts();

    app.UseSwagger();
    app.UseSwaggerUI();

    app.UseSerilogRequestLogging();

    app.UseHttpsRedirection();

    app.UseRouting();

    app.UseCors("HttpScheme");

    app.UseAuthentication();
    app.UseAuthorization();
    app.MapControllers().RequireAuthorization();


    app.MigrateDatabase();
    app.PopulateOptions();

    app.Run();
}
catch (HostAbortedException)
{
    throw;
}
catch (Exception ex)
{
    Log.Fatal(ex, "Unhandled exception");
}
finally
{
    Log.Information("Shut down complete");
    Log.CloseAndFlush();
}
