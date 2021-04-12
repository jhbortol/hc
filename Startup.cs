using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using HealthCheckPOC.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.Extensions.Hosting;
using App.Startup.Custom.Setups;
using Microsoft.AspNetCore.Mvc;

namespace HealthCheckPOC
{
    public class Startup
    {
        public IConfiguration Configuration { get; }
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }
        public void ConfigureServices(IServiceCollection services)
        {
            // Configurando o uso do Entity Framework Core
            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlServer(
                    Configuration.GetConnectionString("SQLConnection")));



            // Ativando o uso de cache via Redis
            services.AddDistributedRedisCache(options =>
            {
                options.Configuration =
                    Configuration.GetConnectionString("CacheRedis");
                options.InstanceName = "Cache-HealthCheckPOC-";
            });


            services.AddControllers();
            services.AddApplicationInsightsTelemetry(Configuration);

            services.AddHealthChecksSetup(Configuration);
        }

        public void Configure(IApplicationBuilder app,
                            IWebHostEnvironment env,
                            IConfiguration conf)
        {

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseRouting();
            app.UseCors();
            app.UseConfigurationHealthChecks();
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });

        }
    }
}