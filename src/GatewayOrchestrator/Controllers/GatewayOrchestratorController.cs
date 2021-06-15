using Dapr.Client;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
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

        [HttpGet]
        public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(HealthCheckResult.Healthy($"Service running version ({serverOptions.AppVersion})"));
        }

        [HttpGet]
        [Route("version")]
        public async Task<IActionResult> GetVersion()
        {
            return Ok($"Service running version ({serverOptions.AppVersion}");
        }

        /// <summary>
        /// Accept HTTP request with a payload and push it to the relevant service bus topic for async processing
        /// </summary>
        /// <param name="deviceId">The id of the device registered with IoT Hub</param>
        /// <param name="payload">The device status message payload in dynamic json format</param>
        /// <returns></returns>
        [HttpPost("{deviceId}")]
        public async Task<IActionResult> ProcessRequest(string deviceId, [FromBody] dynamic payload)
        {
            logger.LogInformation("GatewayOrchestrator: HTTP trigger function processed a request.");

            if (string.IsNullOrEmpty(deviceId))
                return (ActionResult)new BadRequestObjectResult("Invalid request parameters");

            if (payload is null)
                return (ActionResult)new BadRequestObjectResult("Invalid request payload");

            //Here i'm assuming the payload doesn't include the deviceId, adding it here:
            JObject message = JObject.Parse(payload.ToString());
            if (!message.ContainsKey("deviceId"))
                message.Add("devcieId", deviceId);
            var messageJson = message.ToString();
            await daprClient.PublishEventAsync(serverOptions.ServiceBusName, serverOptions.ServiceBusTopic, messageJson);
            
            return (ActionResult)new OkResult();
        }
    }
}
