using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace VerySimple
{
    public class Startup
    {
        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
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

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseStaticFiles();

            app.UseMvc(routes =>
            {
                routes.MapRoute(
                    name: "default",
                    template: "{controller=Home}/{action=Index}/{id?}");
            });

            // app.Run(async (context) =>
            // {
            //     var builder = new StringBuilder();

            //     builder.AppendLine("Server: " + Environment.MachineName);
            //     builder.AppendLine("IsHttps: " + context.Request.IsHttps);
            //     builder.AppendLine("Host: " + context.Request.Headers["Host"]);
            //     builder.AppendLine("X-Forwarded-For: " + context.Request.Headers["X-Forwarded-For"]);
            //     builder.AppendLine("X-Forwarded-Proto: " + context.Request.Headers["X-Forwarded-Proto"]);
            //     builder.AppendLine("X-Forwarded-Port: " + context.Request.Headers["X-Forwarded-Port"]);

            //     await context.Response.WriteAsync(builder.ToString());
            // });
        }
    }
}
