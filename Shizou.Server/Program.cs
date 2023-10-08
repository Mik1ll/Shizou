using System;
using System.IO;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Serilog;
using Shizou.Data;
using Shizou.Data.Database;
using Shizou.Server;
using Shizou.Server.Extensions;
using Shizou.Server.Options;

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
            policy => { policy.WithOrigins("http://localhost:5000"); });
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


    using (var scope = app.Services.CreateScope())
    {
        var services = scope.ServiceProvider;

        var context = services.GetRequiredService<ShizouContext>();
        context.Database.Migrate();

        var options = services.GetRequiredService<IOptionsSnapshot<ShizouOptions>>();
        options.Value.SaveToFile();
    }

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
