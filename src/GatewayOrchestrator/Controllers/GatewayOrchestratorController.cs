using Dapr.Client;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace GatewayOrchestrator.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class GatewayOrchestratorController : ControllerBase, IHealthCheck
    {
        private DaprClient daprClient;
        private ILogger<GatewayOrchestratorController> logger;
        private ServerOptions serverOptions;


        public GatewayOrchestratorController(DaprClient dapr, ServerOptions serverOptions, ILogger<GatewayOrchestratorController> logger)
        {
            daprClient = dapr ?? throw new ArgumentNullException(nameof(dapr));
            this.logger = logger;
            this.serverOptions = serverOptions;
        }

        public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(HealthCheckResult.Healthy($"Service running version ({serverOptions.AppVersion})"));
        }
    }
}
