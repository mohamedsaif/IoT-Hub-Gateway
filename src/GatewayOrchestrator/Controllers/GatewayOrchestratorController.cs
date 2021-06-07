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
        private ILogger<GatewayOrchestratorController> logger;

        public GatewayOrchestratorController(ILogger<GatewayOrchestratorController> logger)
        {
            this.logger = logger;
        }
    }
}
