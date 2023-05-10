using System;
using System.IO;
using AutoMapper;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using Shizou;
using Shizou.Extensions;
using ShizouData;
using ShizouData.Database;

var logTemplate = "{Timestamp:HH:mm:ss} {Level:u3} | {SourceContext} {Message:lj}{NewLine:1}{Exception:1}";
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console(outputTemplate: logTemplate)
    .CreateBootstrapLogger();

try
{
    Directory.CreateDirectory(FilePaths.ApplicationDataDir);

    var builder = WebApplication.CreateBuilder();

    builder.AddShizouOptions()
        .AddShizouLogging(logTemplate)
        .AddShizouApiServices()
        .AddShizouServices()
        .AddAniDbServices()
        .AddShizouProcessors();

    builder.Services.AddHostedService<StartupService>();


    var app = builder.Build();

    if (!app.Environment.IsDevelopment()) app.UseHsts();

    app.UseSwagger();
    app.UseSwaggerUI();

    app.UseSerilogRequestLogging();

    app.UseHttpsRedirection();
    app.UseCors();
    app.UseAuthorization();
    app.MapControllers();


    using (var scope = app.Services.CreateScope())
    {
        var services = scope.ServiceProvider;

        var context = services.GetRequiredService<ShizouContext>();
        context.Database.Migrate();

        var mapper = services.GetRequiredService<IMapper>();
        mapper.ConfigurationProvider.AssertConfigurationIsValid();
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
