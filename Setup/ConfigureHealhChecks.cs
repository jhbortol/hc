using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using HealthChecks.UI.Client;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

namespace App.Startup.Custom.Setups
{
    public static class HealthChecksSetup
    {
        public static void AddHealthChecksSetup(this IServiceCollection services, IConfiguration configuration)
        {

            var healthCheckCustomConfiguration = new HealthCheckCustomConfiguration();

            services.Configure<HealthCheckCustomConfiguration>(options =>
               configuration.GetSection(nameof(HealthCheckCustomConfiguration))
                            .Bind(options));

            configuration.GetSection(nameof(HealthCheckCustomConfiguration))
                         .Bind(healthCheckCustomConfiguration);

            if (healthCheckCustomConfiguration.IsValid())
            {
                services.AddHealthChecksUI(s =>
                {

                    s.AddHealthCheckEndpoint(AssemblyExtension.GetApplicationNameByAssembly, healthCheckCustomConfiguration.UrlHealthCheck ?? "/hc");
                    if (healthCheckCustomConfiguration.MaximumHistoryEntriesPerEndpoint > 0)
                    {
                        s.MaximumHistoryEntriesPerEndpoint(healthCheckCustomConfiguration.MaximumHistoryEntriesPerEndpoint);
                    }
                    s.SetEvaluationTimeInSeconds(healthCheckCustomConfiguration.EvaluationTimeInSeconds);
                    s.SetMinimumSecondsBetweenFailureNotifications(healthCheckCustomConfiguration.MinimumSecondsBetweenFailureNotifications);
                })
                // .AddSqliteStorage("Data Source = healthchecks.db");
                .AddInMemoryStorage();
            }

            var cosmosDBServiceEndPoint = configuration.GetValue<string>("CosmosDBEndpoint");
            var cosmosDBAuthKeyOrResourceToken = configuration.GetValue<string>("CosmosDBAccessKey");
            var cosmosDBConnectionString = $"AccountEndpoint={cosmosDBServiceEndPoint};AccountKey={cosmosDBAuthKeyOrResourceToken};";


            var hcConfiguration = configuration.GetSection(nameof(HealthCheckCustomConfiguration))
                                               .Get<HealthCheckCustomConfiguration>();
            if (hcConfiguration.IsValid())
            {
                services.AddHealthChecks()
                   .AddSqlServer(configuration.GetConnectionString("SQLConnection"), name: "SQLAzure-BD")
                //    .AddCheck("self", () => HealthCheckResult.Healthy())
                        .AddCosmosDb(connectionString: cosmosDBConnectionString, name: "CosmosDB-BD", failureStatus: HealthStatus.Degraded, tags: new string[] { "cosmosdb" }

                    //    .AddCheck
                    //     .AddDbContextCheck<WeatherForecastAccessContext>(
                    //     "name",
                    //     HealthStatus.Degraded,
                    //     tags,
                    //     PerformCosmosHealthCheck())

                    );
                //  .AddRedis(configuration.GetConnectionString("CacheRedis"), name: "cacheRedis");

            }
        }



        public static void UseConfigurationHealthChecks(this IApplicationBuilder app)
        {
            var hcOptions = app.ApplicationServices.GetService<IOptions<HealthCheckCustomConfiguration>>();

            if (hcOptions.Value.IsValid())
            {

                app.UseEndpoints(endpoints =>
                {
                    endpoints.MapHealthChecks("/hc", new HealthCheckOptions()
                    {
                        Predicate = _ => true,
                        ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
                    });

                    endpoints.MapHealthChecksUI(options =>
                    {
                        options.UIPath = "/hc-ui";
                        options.ApiPath = "/hc-ui-api";
                        // options.AddCustomStylesheet("dotnet.css");


                    });

                });

            }
        }

    }
    public class HealthCheck
    {
        public string Status { get; set; }
        public string Component { get; set; }
        public string Description { get; set; }
        public string Exception { get; set; }
        public TimeSpan Duration { get; set; }
        public IDictionary<string, object> Data { get; set; }
        public IEnumerable<string> Tags { get; set; }
    }

    public class HealthCheckCustomConfiguration
    {

        public string UrlHealthCheck { get; set; }
        public bool EnableChecksStandard { get; set; }
        public int EvaluationTimeInSeconds { get; set; }
        public int MinimumSecondsBetweenFailureNotifications { get; set; }
        public int MaximumHistoryEntriesPerEndpoint { get; set; }
        public bool IsValid()
        {
            return EnableChecksStandard;
        }

    }

    public class HealthCheckResponse
    {
        public string Status { get; set; }

        public IEnumerable<HealthCheck> Entries { get; set; }

        public TimeSpan TotalDuration { get; set; }
    }


    internal static class AssemblyExtension
    {


        public static string GetApplicationNameByAssembly
        {

            get
            {
                var entryAssembly = Assembly.GetEntryAssembly().GetName().Name;

                var appCustomName = entryAssembly;


                return appCustomName;

            }
        }

        public static string GetApplicationBuildNumber
        {
            get
            {
                var buildNumber = Assembly.GetEntryAssembly().GetCustomAttribute<AssemblyFileVersionAttribute>().Version;

                return buildNumber;
            }
        }
        public static string GetApplicationVersion
        {

            get
            {
                var applicationVersion = $"{Assembly.GetEntryAssembly().GetName().Version.Major}." +
                                     $"{Assembly.GetEntryAssembly().GetName().Version.Minor}." +
                                     $"{Assembly.GetEntryAssembly().GetName().Version.Build}";


                return applicationVersion;
            }
        }

    }

    // private static Func<WeatherForecastAccessContext, CancellationToken, Task<bool>> PerformCosmosHealthCheck() =>
    //    async (context, _) =>
    //    {
    //        try
    //        {
    //            await context.Database.GetCosmosClient().ReadAccountAsync().ConfigureAwait(false);
    //        }
    //        catch (HttpRequestException)
    //        {
    //            return false;
    //        }
    //        return true;
    //    };

}

