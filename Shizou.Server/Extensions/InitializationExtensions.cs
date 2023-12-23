using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using Microsoft.OpenApi.Models;
using Serilog;
using Shizou.Data;
using Shizou.Data.Database;
using Shizou.Server.AniDbApi;
using Shizou.Server.AniDbApi.RateLimiters;
using Shizou.Server.AniDbApi.Requests.Http;
using Shizou.Server.AniDbApi.Requests.Http.Interfaces;
using Shizou.Server.AniDbApi.Requests.Image;
using Shizou.Server.AniDbApi.Requests.Udp;
using Shizou.Server.AniDbApi.Requests.Udp.Interfaces;
using Shizou.Server.CommandProcessors;
using Shizou.Server.Commands;
using Shizou.Server.Commands.AniDb;
using Shizou.Server.FileCaches;
using Shizou.Server.Options;
using Shizou.Server.Services;
using Shizou.Server.SwaggerFilters;
using UdpAnimeRequest = Shizou.Server.AniDbApi.Requests.Udp.AnimeRequest;
using IUdpAnimeRequest = Shizou.Server.AniDbApi.Requests.Udp.Interfaces.IAnimeRequest;
using HttpAnimeRequest = Shizou.Server.AniDbApi.Requests.Http.AnimeRequest;
using IHttpAnimeRequest = Shizou.Server.AniDbApi.Requests.Http.Interfaces.IAnimeRequest;

namespace Shizou.Server.Extensions;

public static class InitializationExtensions
{
    public static WebApplicationBuilder AddShizouOptions(this WebApplicationBuilder builder)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(FilePaths.OptionsPath)!);
        if (!File.Exists(FilePaths.OptionsPath))
            new ShizouOptions().SaveToFile();
        var assemblyName = Assembly.GetEntryAssembly()?.GetName().Name ?? throw new ApplicationException("No Entry assembly???");
        builder.Configuration
            .AddJsonFile($"appsettings.{assemblyName}.json", true, true)
            .AddJsonFile($"appsettings.{assemblyName}.{builder.Environment.EnvironmentName}.json", true, true)
            .AddJsonFile(FilePaths.OptionsPath, false, true);
        builder.Services.AddOptions<ShizouOptions>()
            .Bind(builder.Configuration.GetSection(ShizouOptions.Shizou))
            .ValidateDataAnnotations()
            .ValidateOnStart();
        return builder;
    }

    public static WebApplicationBuilder AddShizouLogging(this WebApplicationBuilder builder)
    {
        builder.Host.UseSerilog((ctx, cfg) => cfg
            .ReadFrom.Configuration(ctx.Configuration)
            .ConfigureSerilog());
        return builder;
    }

    public static WebApplicationBuilder AddWorkerServices(this WebApplicationBuilder builder)
    {
        builder.Host.UseWindowsService(cfg => cfg.ServiceName = "Shizou")
            .UseSystemd();
        return builder;
    }

    public static WebApplication MigrateDatabase(this WebApplication app)
    {
        using var context = app.Services.GetRequiredService<IShizouContextFactory>().CreateDbContext();
        context.Database.Migrate();
        return app;
    }

    public static WebApplication PopulateOptions(this WebApplication app)
    {
        var options = app.Services.GetRequiredService<IOptions<ShizouOptions>>();
        options.Value.SaveToFile();
        if (File.ReadAllText(FilePaths.SchemaPath) != ShizouOptions.Schema)
            File.WriteAllText(FilePaths.SchemaPath, ShizouOptions.Schema);
        return app;
    }

    public static LoggerConfiguration ConfigureSerilog(this LoggerConfiguration cfg)
    {
        var logTemplate = "{Timestamp:HH:mm:ss} {Level:u3} | {SourceContext} {Message:lj}{NewLine:1}{Exception:1}";
        return cfg
            .WriteTo.Console(outputTemplate: logTemplate)
            .WriteTo.File(Path.Combine(FilePaths.LogsDir, ".log"), outputTemplate: logTemplate, rollingInterval: RollingInterval.Day)
            .WriteTo.Seq("http://localhost:5341")
            .Enrich.FromLogContext()
            .Enrich.WithThreadId()
            .Enrich.WithThreadName()
            .Filter.ByExcluding(logEvent => logEvent.IsSuppressed());
    }

    public static IServiceCollection AddShizouServices(this IServiceCollection services)
    {
        services.AddDbContextFactory<ShizouContext>()
            .AddIdentity<IdentityUser, IdentityRole>(options =>
            {
                options.SignIn.RequireConfirmedEmail = false;
                options.Password.RequireDigit = false;
                options.Password.RequireLowercase = false;
                options.Password.RequireUppercase = false;
                options.Password.RequireNonAlphanumeric = false;
            })
            .AddEntityFrameworkStores<ShizouContext>()
            .AddDefaultTokenProviders().Services
            .ConfigureApplicationCookie(opts =>
            {
                opts.LoginPath = "/Account/Login";
                opts.LogoutPath = "/Account/Logout";
                opts.Events.OnRedirectToLogin = context =>
                {
                    if (context.Request.Path.StartsWithSegments(Constants.ApiPrefix))
                    {
                        context.Response.Clear();
                        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                        return Task.CompletedTask;
                    }

                    context.Response.Redirect(context.RedirectUri);
                    return Task.CompletedTask;
                };
                opts.Cookie.Name = Constants.IdentityCookieName;
            })
            .AddAuthorization(options =>
            {
                options.FallbackPolicy = new AuthorizationPolicyBuilder()
                    .RequireAuthenticatedUser()
                    .Build();
            })
            .AddScoped<IShizouContext, ShizouContext>(p => p.GetRequiredService<ShizouContext>())
            .AddSingleton<IShizouContextFactory, ShizouContextFactory>(p =>
                new ShizouContextFactory(p.GetRequiredService<IDbContextFactory<ShizouContext>>()))
            .AddHostedService<StartupService>()
            .AddTransient<AniDbFileResultCache>()
            .AddTransient<HttpAnimeResultCache>()
            .AddTransient<HashCommand>()
            .AddTransient<NoopCommand>()
            .AddTransient<AnimeCommand>()
            .AddTransient<ProcessCommand>()
            .AddTransient<SyncMyListCommand>()
            .AddTransient<UpdateMyListCommand>()
            .AddTransient<AddMissingMyListEntriesCommand>()
            .AddTransient<GetImageCommand>()
            .AddTransient<RestoreMyListBackupCommand>()
            .AddSingleton<CommandService>()
            .AddHostedService<CommandService>(p => p.GetRequiredService<CommandService>())
            .AddTransient<ImportService>()
            .AddTransient<WatchStateService>()
            .AddTransient<ImageService>()
            .AddTransient<HashService>()
            .AddTransient<SubtitleService>()
            .AddSingleton<AnimeTitleSearchService>() // Uses an in memory cache
            .AddSingleton<MyAnimeListService>() // Has State and Code challenge shared state
            .AddTransient<FfmpegService>()
            .AddTransient<AnimeService>();
        return services;
    }

    public static IServiceCollection AddShizouProcessors(this IServiceCollection services)
    {
        services.AddProcessor<AniDbUdpProcessor>()
            .AddProcessor<HashProcessor>()
            .AddProcessor<AniDbHttpProcessor>()
            .AddProcessor<GeneralProcessor>()
            .AddProcessor<ImageProcessor>()
            .Configure<HostOptions>(opts => opts.ShutdownTimeout = TimeSpan.FromSeconds(30));
        return services;
    }

    public static IServiceCollection AddAniDbServices(this IServiceCollection services)
    {
        services.AddSingleton<AniDbUdpState>()
            .AddSingleton<UdpRateLimiter>()
            .AddSingleton<AniDbHttpState>()
            .AddSingleton<HttpRateLimiter>()
            .AddSingleton<ImageRateLimiter>()
            .AddTransient<IUdpAnimeRequest, UdpAnimeRequest>()
            .AddGenericFactory<IAuthRequest, AuthRequest>()
            .AddGenericFactory<ILogoutRequest, LogoutRequest>()
            .AddTransient<IEpisodeRequest, EpisodeRequest>()
            .AddTransient<IFileRequest, FileRequest>()
            .AddTransient<IGenericRequest, GenericRequest>()
            .AddTransient<IMyListAddRequest, MyListAddRequest>()
            .AddTransient<IPingRequest, PingRequest>()
            .AddTransient<IMyListEntryRequest, MyListEntryRequest>()
            .AddTransient<IHttpAnimeRequest, HttpAnimeRequest>()
            .AddTransient<IMyListRequest, MyListRequest>()
            .AddTransient<ImageRequest>()
            .AddHttpClient("gzip")
            .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler { AutomaticDecompression = DecompressionMethods.GZip });
        return services;
    }

    public static IServiceCollection AddShizouApiServices(this IServiceCollection services)
    {
        services.AddControllers(opts => opts.SuppressImplicitRequiredAttributeForNonNullableReferenceTypes = true)
            .AddJsonOptions(opt => opt.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter())).Services
            .AddSwaggerGen(opt =>
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
                    Description = "Asp Identity Token",
                    Flows = new OpenApiOAuthFlows
                    {
                        Implicit = new OpenApiOAuthFlow
                        {
                            AuthorizationUrl = new Uri($"{Constants.ApiPrefix}/Account/Login", UriKind.Relative),
                            Scopes = new Dictionary<string, string>()
                        }
                    }
                });
                opt.OperationFilter<SecurityOperationFilter>();
            })
            .AddTransient<IContentTypeProvider, FileExtensionContentTypeProvider>();
        return services;
    }

    public static IApplicationBuilder UseIdentityCookieParameter(this IApplicationBuilder app)
    {
        return app.Use((context, next) =>
        {
            if (!context.Request.Cookies.ContainsKey(Constants.IdentityCookieName) &&
                context.Request.Query.TryGetValue(Constants.IdentityCookieName, out var identityParam))
                context.Request.Headers.Cookie = new StringValues(
                    (string.IsNullOrWhiteSpace(context.Request.Headers.Cookie) ? string.Empty : $"{context.Request.Headers.Cookie}; ") +
                    $"{Constants.IdentityCookieName}={identityParam}");

            return next.Invoke();
        });
    }

    // ReSharper disable once UnusedMethodReturnValue.Local
    private static IServiceCollection AddProcessor<TProcessor>(this IServiceCollection services)
        where TProcessor : CommandProcessor
    {
        services
            .AddSingleton<TProcessor>()
            .AddHostedService<TProcessor>(p => p.GetRequiredService<TProcessor>())
            .AddSingleton<CommandProcessor, TProcessor>(p => p.GetRequiredService<TProcessor>());
        return services;
    }

    // ReSharper disable once UnusedMethodReturnValue.Local
    private static IServiceCollection AddGenericFactory<TInterface, TImplementation>(this IServiceCollection services)
        where TInterface : class
        where TImplementation : class, TInterface
    {
        services.AddTransient<TInterface, TImplementation>()
            .AddSingleton<Func<TInterface>>(x => x.GetRequiredService<TInterface>);
        return services;
    }

    // ReSharper disable once UnusedMember.Local
    private static IServiceCollection AddFactory<TInterface, TImplementation, TFactoryInterface, TFactoryImplementation>(
        this IServiceCollection services)
        where TInterface : class
        where TImplementation : class, TInterface
        where TFactoryInterface : class
        where TFactoryImplementation : class, TFactoryInterface
    {
        services.AddTransient<TInterface, TImplementation>()
            .AddSingleton<Func<TInterface>>(x => x.GetRequiredService<TInterface>)
            .AddSingleton<TFactoryInterface, TFactoryImplementation>();
        return services;
    }
}
