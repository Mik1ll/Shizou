using System.IO;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Hosting;
using Serilog;
using Shizou.Data;
using Shizou.Server.Extensions;

Log.Logger = new LoggerConfiguration()
    .ConfigureSerilog()
    .CreateBootstrapLogger();

Directory.CreateDirectory(FilePaths.ApplicationDataDir);

var builder = WebApplication.CreateBuilder();

builder.AddShizouOptions()
    .AddShizouLogging()
    .AddWorkerServices();

builder.Services
    .AddShizouServices()
    .AddShizouProcessors()
    .AddAniDbServices()
    .AddShizouApiServices();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
    app.UseHsts();

app.UseSwagger();
app.UseSwaggerUI();

app.UseSerilogRequestLogging();

app.UseHttpsRedirection();

app.UseRouting();

app.UseIdentityCookieParameter();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.PopulateOptions();
app.MigrateDatabase();

app.Run();
