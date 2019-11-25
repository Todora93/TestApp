using System;
using MyActorService.Interfaces;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace WebService
{
    public class Startup
    {
        public static IServiceProvider serviceProvider;
        public static IHubContext<MyHub> hubContext;

        public static IUserConnectionManager userConnectionManager;

        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllersWithViews();
            services.AddSignalR();

            services.AddSingleton<IUserConnectionManager, UserConnectionManager>();

            services.AddSingleton<IUserIdProvider, CustomUserIdProvider>();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, IServiceProvider serviceProvider)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
            }
            app.UseStaticFiles();

            app.UseRouting();

            app.UseAuthorization();

            app.Use(async (context, next) =>
            {
                serviceProvider = context.RequestServices;
                hubContext = context.RequestServices.GetRequiredService<IHubContext<MyHub>>();
                userConnectionManager = context.RequestServices.GetRequiredService<IUserConnectionManager>();

                await next.Invoke();
            });

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllerRoute(
                    name: "default",
                    pattern: "{controller=Home}/{action=Index}/{id?}");
                endpoints.MapHub<MyHub>("/myHub");
            });
        }
    }
}
