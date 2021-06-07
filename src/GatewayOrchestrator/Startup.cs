using GatewayOrchestrator.Controllers;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GatewayOrchestrator
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
            var options = new ServerOptions();
            Configuration.GetSection(nameof(ServerOptions)).Bind(options);
            services.AddSingleton<ServerOptions>(options);

            RegisterAppInsights(services);

            services.AddControllers()
                .AddDapr();
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "Gateway Orchestrator", Version = "v1" });
            });

            services.AddHealthChecks()
                .AddCheck<GatewayOrchestratorController>("DefaultHealth");

        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseSwagger();
                app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Gateway Orchestrator v1"));
            }

            app.UseHttpsRedirection();

            app.UseRouting();

            app.UseAuthorization();

            app.UseCloudEvents();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapSubscribeHandler();
                endpoints.MapControllers();
                endpoints.MapHealthChecks("/health");
            });
        }

        private void RegisterAppInsights(IServiceCollection services)
        {
            string appInsightsConnection = Configuration["APPINSIGHTS_CONNECTIONSTRING"];
            services.AddApplicationInsightsTelemetry(appInsightsConnection);
            services.AddApplicationInsightsKubernetesEnricher();
        }
    }
}
