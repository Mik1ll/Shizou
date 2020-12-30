using System;
using System.Reflection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;
using Serilog;
using Shizou.Database;
using Shizou.Options;
using Shizou.Repositories;
using Shizou.Services;

namespace Shizou
{
    public class Startup
    {
        public IConfiguration Configuration { get; }

        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.Configure<ShizouOptions>(Configuration.GetSection(ShizouOptions.Shizou));
            services.AddControllers();
            services.AddSwaggerGen(options =>
            {
                options.SwaggerDoc("v1", new OpenApiInfo { Title = "Shizou", Version = "v1" });
                // Set the comments path for the Swagger JSON and UI.
                string? xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
                string? xmlPath = System.IO.Path.Combine(AppContext.BaseDirectory, xmlFile);
                options.IncludeXmlComments(xmlPath);
            });

            services.AddScoped<IDatabase, SQLiteDatabase>();
            services.AddScoped<IImportFolderRepository, ImportFolderRepository>();
            services.AddScoped<IImportFolderService, ImportFolderService>();
            services.AddHostedService<StartupService>();
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

            app.UseEndpoints(endpoints => endpoints.MapControllers());
        }
    }
}