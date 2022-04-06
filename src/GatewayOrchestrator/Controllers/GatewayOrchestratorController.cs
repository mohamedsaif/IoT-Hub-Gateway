using Dapr.Client;
using GatewayOrchestrator.Models;
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
            return Task.FromResult(HealthCheckResult.Healthy(
                JsonConvert.SerializeObject(new { Message = $"Service running version ({serverOptions.AppVersion})" })));
        }

        [HttpGet]
        [Route("version")]
        public async Task<IActionResult> GetVersion()
        {
            return Ok(new { Message = $"Service running version ({serverOptions.AppVersion})" });
        }

        /// <summary>
        /// Accept HTTP request with a payload and push it to the relevant service bus topic for async processing
        /// </summary>
        /// <param name="targetPlatform">As orchestrator is designed to target multiple processing pipelines, currently the only implemented value is IoTHubServer</param>
        /// <param name="payload">The device status message payload in dynamic json format</param>
        /// <returns></returns>
        [HttpPost("{targetPlatform}")]
        public async Task<IActionResult> ProcessRequest(string targetPlatform, [FromBody] dynamic payload)
        {
            logger.LogInformation("GatewayOrchestrator: HTTP trigger starting a request.");

            if (string.IsNullOrEmpty(targetPlatform))
                return (ActionResult)new BadRequestObjectResult("Invalid request parameters");

            if (payload is null)
                return (ActionResult)new BadRequestObjectResult("Invalid request payload");

            
            switch (targetPlatform)
            {
                case "IoTHubServer":
                    JObject message = JObject.Parse(payload.ToString());
                    // Validate that the payload include the defined EntityIdAttributeName
                    var idToken = message.SelectToken(serverOptions.EntityIdAttributeName);
                    string deviceId = idToken != null ? idToken.Value<string>() : string.Empty;
                    if (string.IsNullOrEmpty(deviceId))
                        throw new ArgumentException($"Invalid payload due to no id at ({serverOptions.EntityIdAttributeName})");

                    var messageJson = JsonConvert.SerializeObject(message);
                    await daprClient.PublishEventAsync<string>(serverOptions.ServiceBusName, serverOptions.ServiceBusTopic, messageJson);
                    logger.LogInformation($"GatewayOrchestrator: HTTP trigger completed a DEVICE request for enitityId: ({deviceId})");
                    break;
                //case "AnotherTargetSystem":
                    //TODO: add business logic to handle publishing to the relevant bus
                default:
                    throw new ArgumentException("Input target platform");
            }
            
            
            
            return (ActionResult)new OkResult();
        }
    }
}
