using Dapr.Client;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GatewayOrchestrator.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class GatewayOrchestratorController : ControllerBase
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
    }
}
