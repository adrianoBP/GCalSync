using GCalSync.Workers;
using Hangfire;
using Hangfire.MemoryStorage;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.IO;

namespace GCalSync
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
            services.AddControllersWithViews();

            services.AddAuthentication()
                .AddGoogle(options =>
                {
                    IConfigurationSection googleAuthNSection =
                        Configuration.GetSection("Authentication:Google");

                    options.ClientId = googleAuthNSection["ClientId"];
                    options.ClientSecret = googleAuthNSection["ClientSecret"];
                });

            services.AddHangfireServer();
            services.AddHangfire(config => config.UseMemoryStorage(new MemoryStorageOptions { FetchNextJobTimeout = TimeSpan.FromHours(24) }));
            services.AddSignalR();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            var builder = new ConfigurationBuilder()
              .SetBasePath(Directory.GetCurrentDirectory())
              .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);

            ApplicationSettings.Init(builder.Build());

            app.UseHsts();

            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllerRoute(
                    name: "default",
                    pattern: "{controller=Home}/{action=Index}/{id?}");
            });

            app.UseHangfireDashboard("");

            SetSchedules();
        }

        private static void SetSchedules()
        {
            CleanupWorker cleanupWorker = new();
            AccountWorker accountWorker = new();
            SyncWorker syncWorker = new();

            RecurringJob.AddOrUpdate("CleanupWorkerFull", () => cleanupWorker.FullCleanup(), Cron.Never);
            RecurringJob.AddOrUpdate("CleanupWorkerFrom", () => cleanupWorker.ClearFromAccount(), Cron.Never);
            RecurringJob.AddOrUpdate("CleanupWorkerTo", () => cleanupWorker.ClearToAccount(), Cron.Never);

            RecurringJob.AddOrUpdate("AccountWorkerFrom", () => accountWorker.AddFromAccount(), Cron.Never);
            RecurringJob.AddOrUpdate("AccountWorkerTo", () => accountWorker.AddToAccount(), Cron.Never);

            RecurringJob.AddOrUpdate("SyncWorker", () => syncWorker.StartSync(), Cron.Never);
        }
    }
}
