using GatewayOrchestrator.Controllers;
using GatewayOrchestrator.Utils;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Extensions;
using Microsoft.OpenApi;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.Swagger;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
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
            options.AppVersion = string.IsNullOrEmpty(System.Environment.GetEnvironmentVariable("AppVersion")) ? "NA" : System.Environment.GetEnvironmentVariable("AppVersion");
            services.AddSingleton<ServerOptions>(options);

            RegisterAppInsights(services);

            services.AddControllers()
                .AddDapr();
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "Gateway Orchestrator", Version = "v1" });
            });

            services.AddHealthChecks()
                .AddCheck<ServiceHealthCheck>("default");

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

            app.UseRouting();

            app.UseAuthorization();

            app.UseCloudEvents();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapSubscribeHandler();
                endpoints.MapControllers();
                endpoints.MapHealthChecks("/healthz");
                endpoints.MapHealthChecks("/health-details", new HealthCheckOptions
                {
                    ResponseWriter = ServiceHealthCheck.WriteResponse
                });
            });

            if(env.IsDevelopment())
            {
                ISwaggerProvider sw = (ISwaggerProvider)app.ApplicationServices.GetService(typeof(ISwaggerProvider));
                OpenApiDocument doc = sw.GetSwagger("v1");
                var output = doc.SerializeAsJson(OpenApiSpecVersion.OpenApi3_0);
                string openApiSpecsFolder = Path.Combine(Directory.GetCurrentDirectory(), "OpenApiSpecs");
                string swaggerFile = Path.Combine(openApiSpecsFolder, "swagger.json");
                File.WriteAllText(swaggerFile, output);
            }
        }

        private void RegisterAppInsights(IServiceCollection services)
        {
            services.AddApplicationInsightsTelemetry(Configuration);
            services.AddApplicationInsightsKubernetesEnricher();
        }
    }
}
