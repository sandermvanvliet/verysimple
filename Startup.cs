using System.IO;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Caching.Distributed;
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
            var dataProtectionBuilder = (IDataProtectionBuilder)services
                .AddDataProtection(c => c.ApplicationDiscriminator = "VerySimple")
                .SetApplicationName("VerySimple")
                .PersistKeysToFileSystem(new DirectoryInfo("/dpapi"));

            var serverName = System.Environment.GetEnvironmentVariable("MYSQLSERVERNAME");
            if(string.IsNullOrEmpty(serverName))
            {
                serverName = "localhost";
            }

            System.Console.WriteLine("Using MYSQLSERVERNAME: " + serverName);
            var connectionString = $"Server={serverName};Database=sessionstate;Username=sessionStateUser;Password=aaabbb";

            services.TryAddSingleton<IDistributedCache>(new MyDistributedCache(connectionString));
        
            services.AddSession();
            services.AddMvc();
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

            var serverName = System.Environment.GetEnvironmentVariable("MYSQLSERVERNAME");
            loggerFactory.CreateLogger("startup").LogInformation("Using MYSQLSERVERNAME: " + serverName);

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseStaticFiles();

            app.UseSession();

            app.UseMvc(routes =>
            {
                routes.MapRoute(
                    name: "default",
                    template: "{controller=Home}/{action=Index}/{id?}");
            });
        }
    }
}
