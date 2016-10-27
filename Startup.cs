using System.IO;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

namespace VerySimple
{
    public class Startup
    {
        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
            var configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", true)
                .AddEnvironmentVariables()
                .Build();

            InitializeConfigurationDefaults(configuration);

            var dataProtectionBuilder = (IDataProtectionBuilder)services
                .AddDataProtection(c => c.ApplicationDiscriminator = "VerySimple")
                .SetApplicationName("VerySimple")
                .PersistKeysToFileSystem(new DirectoryInfo(configuration["DPAPI_PATH"]));

            var serverName = configuration["MYSQLSERVERNAME"];
            var connectionString = $"Server={serverName};Database=sessionstate;Username=sessionStateUser;Password=aaabbb;SslMode=None";

            services.TryAddSingleton<IConfiguration>(configuration);
            services.TryAddSingleton<IDistributedCache>(new MyDistributedCache(connectionString));

            services.AddSession();
            services.AddMvc();
        }

        private void InitializeConfigurationDefaults(IConfigurationRoot configuration)
        {
            if(string.IsNullOrEmpty(configuration["MYSQLSERVERNAME"]))
            {
                configuration["MYSQLSERVERNAME"] = "localhost";
            }

            if(string.IsNullOrEmpty(configuration["DPAPI_PATH"]))
            {
                configuration["DPAPI_PATH"] = "dpapi";
            }
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
        {
            app.UseForwardedHeaders(
                        new ForwardedHeadersOptions
                        {
                            ForwardedHeaders = ForwardedHeaders.XForwardedProto | ForwardedHeaders.XForwardedFor
                        });

            loggerFactory.AddConsole();

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseStaticFiles();

            app.UseSession();

            app.Map("/health", HealthCheckEndpoint);

            app.UseMvc(routes =>
            {
                routes.MapRoute(
                    name: "default",
                    template: "{controller=Home}/{action=Index}/{id?}");
            });
        }

        private static void HealthCheckEndpoint(IApplicationBuilder appBuilder)
        {
            appBuilder.Run(async context =>
            {
                var configuration = (IConfiguration)context.RequestServices.GetService(typeof(IConfiguration));
                
                var isHealthy = HealthChecker.Check(configuration);

                // 200 for OK, 503 for Service Unavailable
                context.Response.StatusCode = isHealthy ? 200 : 503;
                
                await context.Response.Body.WriteAsync(new byte[0], 0, 0);
            });
        }
    }
}
