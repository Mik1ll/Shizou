using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;
using Serilog;
using Shizou.AniDbApi;
using Shizou.CommandProcessors;
using Shizou.Commands;
using Shizou.Database;
using Shizou.Import;
using Shizou.Options;
using Shizou.SwaggerFilters;
using Swashbuckle.AspNetCore.Filters;

namespace Shizou
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.Configure<ShizouOptions>(Configuration.GetSection(ShizouOptions.Shizou));
            services.AddControllers(options =>
            {
                var (inputfmtr, outputfmtr) = GetFormatters();
                options.InputFormatters.Insert(0, inputfmtr);
                options.OutputFormatters.Add(outputfmtr);
            });
            services.AddSwaggerGen(options =>
            {
                //options.CustomOperationIds(e => $"{e.ActionDescriptor.RouteValues["controller"]}_{e.HttpMethod}");
                options.SwaggerDoc("v1", new OpenApiInfo {Title = "Shizou", Version = "v1"});

                options.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, $"{Assembly.GetExecutingAssembly().GetName().Name}.xml"));

                options.DocumentFilter<JsonPatchDocumentFilter>();
                options.OperationFilter<ClearContentTypeFilter>();
                options.ExampleFilters();
            });
            services.AddSwaggerExamplesFromAssemblyOf<JsonPatchExample>();
            services.AddHostedService<StartupService>();
            services.AddDbContext<ShizouContext>();
            services.AddScoped<CommandManager>();
            services.AddSingleton<AniDbUdp>();
            services.AddSingleton<UdpRateLimiter>();
            services.AddSingleton<AniDbUdpProcessor>();
            services.AddSingleton<HashProcessor>();
            services.AddScoped<Importer>();
            services.AddSingleton<IHostedService>(p => p.GetRequiredService<AniDbUdpProcessor>());
            services.AddSingleton<IHostedService>(p => p.GetRequiredService<HashProcessor>());
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseSwagger();
                app.UseSwaggerUI(options => options.SwaggerEndpoint("/swagger/v1/swagger.json", "Shizou v1"));
            }

            //app.UseHttpsRedirection();

            app.UseSerilogRequestLogging();
            app.UseRouting();

            app.UseAuthorization();

            app.UseEndpoints(endpoints => { endpoints.MapControllers(); });
        }

        private static (IInputFormatter, IOutputFormatter) GetFormatters()
        {
            var builder = new ServiceCollection()
                .AddLogging()
                .AddMvcCore()
                .AddNewtonsoftJson()
                .AddXmlDataContractSerializerFormatters()
                .Services.BuildServiceProvider();

            var mvcOptions = builder
                .GetRequiredService<IOptions<MvcOptions>>()
                .Value;

            return (mvcOptions.InputFormatters.OfType<NewtonsoftJsonPatchInputFormatter>().Single(),
                mvcOptions.OutputFormatters.OfType<XmlDataContractSerializerOutputFormatter>().Single());
        }
    }
}
