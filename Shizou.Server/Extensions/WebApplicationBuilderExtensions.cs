using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;
using Serilog;
using Shizou.Data;
using Shizou.Data.Database;
using Shizou.Data.Enums;
using Shizou.Server.AniDbApi;
using Shizou.Server.AniDbApi.RateLimiters;
using Shizou.Server.AniDbApi.Requests.Http;
using Shizou.Server.AniDbApi.Requests.Image;
using Shizou.Server.AniDbApi.Requests.Udp;
using Shizou.Server.AniDbApi.Requests.Udp.Notify;
using Shizou.Server.CommandProcessors;
using Shizou.Server.Commands;
using Shizou.Server.Commands.AniDb;
using Shizou.Server.FileCaches;
using Shizou.Server.Options;
using Shizou.Server.Services;
using Shizou.Server.SwaggerFilters;
using UdpAnimeRequest = Shizou.Server.AniDbApi.Requests.Udp.AnimeRequest;
using HttpAnimeRequest = Shizou.Server.AniDbApi.Requests.Http.AnimeRequest;

namespace Shizou.Server.Extensions;

public static class WebApplicationBuilderExtensions
{
    public static WebApplicationBuilder AddShizouOptions(this WebApplicationBuilder builder)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(FilePaths.OptionsPath)!);
        if (!File.Exists(FilePaths.OptionsPath))
            new ShizouOptions().SaveToFile();
        builder.Configuration.AddJsonFile(FilePaths.OptionsPath, false, true);
        builder.Services.AddOptions<ShizouOptions>()
            .Bind(builder.Configuration.GetSection(ShizouOptions.Shizou))
            .ValidateDataAnnotations()
            .ValidateOnStart();
        return builder;
    }

    public static WebApplicationBuilder AddShizouLogging(this WebApplicationBuilder builder, string logTemplate)
    {
        builder.Host.UseSerilog((ctx, cfg) => cfg.ReadFrom.Configuration(ctx.Configuration)
            .WriteTo.Console(outputTemplate: logTemplate)
            .WriteTo.File(Path.Combine(FilePaths.LogsDir, ".log"), outputTemplate: logTemplate, rollingInterval: RollingInterval.Day)
            .WriteTo.Seq("http://localhost:5341")
            .Enrich.FromLogContext()
            .Filter.ByExcluding(logEvent => logEvent.IsSuppressed()));
        return builder;
    }

    public static WebApplicationBuilder AddShizouServices(this WebApplicationBuilder builder)
    {
        builder.Services.AddDbContextFactory<ShizouContext>();
        builder.Services.AddIdentity<IdentityUser, IdentityRole>(options =>
            {
                options.SignIn.RequireConfirmedEmail = false;
                options.Password.RequireDigit = false;
                options.Password.RequireLowercase = false;
                options.Password.RequireUppercase = false;
                options.Password.RequireNonAlphanumeric = false;
            })
            .AddEntityFrameworkStores<ShizouContext>()
            .AddDefaultTokenProviders();

        builder.Services.ConfigureApplicationCookie(opts =>
        {
            opts.LoginPath = "/Account/Login";
            opts.LogoutPath = "/Account/Logout";
            opts.Events.OnRedirectToLogin = context =>
            {
                if (context.Request.Path.StartsWithSegments("/api"))
                {
                    context.Response.Clear();
                    context.Response.StatusCode = StatusCodes.Status403Forbidden;
                    return Task.CompletedTask;
                }

                context.Response.Redirect(context.RedirectUri);
                return Task.CompletedTask;
            };
            opts.Cookie.Name = Constants.IdentityCookieName;
        });


        builder.Services.AddScoped<AniDbFileResultCache>();
        builder.Services.AddScoped<HttpAnimeResultCache>();

        builder.Services.AddTransient<HashCommand>();
        builder.Services.AddTransient<NoopCommand>();
        builder.Services.AddTransient<AnimeCommand>();
        builder.Services.AddTransient<ProcessCommand>();
        builder.Services.AddTransient<SyncMyListCommand>();
        builder.Services.AddTransient<UpdateMyListCommand>();
        builder.Services.AddTransient<AddMissingMyListEntriesCommand>();
        builder.Services.AddTransient<ExportCommand>();
        builder.Services.AddTransient<ExportPollCommand>();
        builder.Services.AddTransient<SyncMyListFromExportCommand>();
        builder.Services.AddTransient<GetImageCommand>();
        builder.Services.AddTransient<RestoreMyListBackupCommand>();

        builder.Services.AddScoped<CommandService>();
        builder.Services.AddScoped<ImportService>();
        builder.Services.AddScoped<WatchStateService>();
        builder.Services.AddScoped<ImageService>();
        builder.Services.AddScoped<MyAnimeListService>();
        return builder;
    }

    public static WebApplicationBuilder AddShizouProcessors(this WebApplicationBuilder builder)
    {
        builder.Services.AddSingleton<CommandProcessor, AniDbUdpProcessor>();
        builder.Services.AddSingleton(p => (AniDbUdpProcessor)p.GetServices<CommandProcessor>().First(s => s.QueueType == QueueType.AniDbUdp));
        builder.Services.AddSingleton<IHostedService>(p => p.GetServices<CommandProcessor>().First(s => s.QueueType == QueueType.AniDbUdp));

        builder.Services.AddSingleton<CommandProcessor, HashProcessor>();
        builder.Services.AddSingleton(p => (HashProcessor)p.GetServices<CommandProcessor>().First(s => s.QueueType == QueueType.Hash));
        builder.Services.AddSingleton<IHostedService>(p => p.GetServices<CommandProcessor>().First(s => s.QueueType == QueueType.Hash));

        builder.Services.AddSingleton<CommandProcessor, AniDbHttpProcessor>();
        builder.Services.AddSingleton(p => (AniDbHttpProcessor)p.GetServices<CommandProcessor>().First(s => s.QueueType == QueueType.AniDbHttp));
        builder.Services.AddSingleton<IHostedService>(p => p.GetServices<CommandProcessor>().First(s => s.QueueType == QueueType.AniDbHttp));

        builder.Services.AddSingleton<CommandProcessor, GeneralProcessor>();
        builder.Services.AddSingleton(p => (GeneralProcessor)p.GetServices<CommandProcessor>().First(s => s.QueueType == QueueType.General));
        builder.Services.AddSingleton<IHostedService>(p => p.GetServices<CommandProcessor>().First(s => s.QueueType == QueueType.General));

        builder.Services.AddSingleton<CommandProcessor, ImageProcessor>();
        builder.Services.AddSingleton(p => (ImageProcessor)p.GetServices<CommandProcessor>().First(s => s.QueueType == QueueType.Image));
        builder.Services.AddSingleton<IHostedService>(p => p.GetServices<CommandProcessor>().First(s => s.QueueType == QueueType.Image));
        return builder;
    }

    public static WebApplicationBuilder AddAniDbServices(this WebApplicationBuilder builder)
    {
        builder.Services.AddSingleton<AniDbUdpState>();
        builder.Services.AddSingleton<UdpRateLimiter>();
        builder.Services.AddSingleton<AniDbHttpState>();
        builder.Services.AddSingleton<HttpRateLimiter>();
        builder.Services.AddSingleton<ImageRateLimiter>();

        builder.Services.AddTransient<UdpAnimeRequest>();
        builder.Services.AddTransient<AuthRequest>();
        builder.Services.AddTransient<EpisodeRequest>();
        builder.Services.AddTransient<FileRequest>();
        builder.Services.AddTransient<GenericRequest>();
        builder.Services.AddTransient<LogoutRequest>();
        builder.Services.AddTransient<MyListAddRequest>();
        builder.Services.AddTransient<PingRequest>();
        builder.Services.AddTransient<NotifyListRequest>();
        builder.Services.AddTransient<NotifyGetRequest>();
        builder.Services.AddTransient<NotifyAckRequest>();
        builder.Services.AddTransient<MessageGetRequest>();
        builder.Services.AddTransient<MessageAckRequest>();
        builder.Services.AddTransient<MyListExportRequest>();
        builder.Services.AddTransient<MyListEntryRequest>();

        builder.Services.AddTransient<HttpAnimeRequest>();
        builder.Services.AddTransient<MyListRequest>();

        builder.Services.AddTransient<ImageRequest>();

        builder.Services.AddScoped<HttpRequestFactory>();
        builder.Services.AddScoped<UdpRequestFactory>();

        builder.Services.AddHttpClient("gzip")
            .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler { AutomaticDecompression = DecompressionMethods.GZip });
        return builder;
    }

    public static WebApplicationBuilder AddShizouApiServices(this WebApplicationBuilder builder)
    {
        builder.Services.AddControllers(opts => opts.SuppressImplicitRequiredAttributeForNonNullableReferenceTypes = true)
            .AddJsonOptions(opt => opt.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
        builder.Services.AddSwaggerGen(opt =>
        {
            opt.SchemaGeneratorOptions.UseInlineDefinitionsForEnums = true;
            opt.SchemaGeneratorOptions.SupportNonNullableReferenceTypes = true;
            opt.OrderActionsBy(apiDesc =>
                $"{apiDesc.ActionDescriptor.RouteValues["controller"]}_{apiDesc.RelativePath?.Length ?? 0:d3}_{apiDesc.HttpMethod switch { "GET" => "0", "PUT" => "1", "POST" => "2", "DELETE" => "3", _ => "4" }}");
            opt.EnableAnnotations();
            opt.AddSecurityDefinition("AspIdentity", new OpenApiSecurityScheme
            {
                Type = SecuritySchemeType.ApiKey,
                Name = Constants.IdentityCookieName,
                In = ParameterLocation.Cookie,
                Description = "Asp Identity token",
                Flows = new OpenApiOAuthFlows
                {
                    Implicit = new OpenApiOAuthFlow
                    {
                        AuthorizationUrl = new Uri("/api/Account/Login", UriKind.Relative),
                        Scopes = new Dictionary<string, string>()
                    }
                }
            });
            opt.OperationFilter<SecurityOperationFilter>();
        });
        return builder;
    }
}