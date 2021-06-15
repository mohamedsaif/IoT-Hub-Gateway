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
        private const string EntityIdAttributeName = "deviceId";
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
        /// <param name="entityId">The id of the entity subject of the request, this will be device id registered with IoT Hub in case of IoT Hub server integation</param>
        /// <param name="payload">The device status message payload in dynamic json format</param>
        /// <returns></returns>
        [HttpPost("{entityId}")]
        public async Task<IActionResult> ProcessRequest(string entityId, [FromBody] dynamic payload)
        {
            logger.LogInformation("GatewayOrchestrator: HTTP trigger function processed a request.");

            if (string.IsNullOrEmpty(entityId))
                return (ActionResult)new BadRequestObjectResult("Invalid request parameters");

            if (payload is null)
                return (ActionResult)new BadRequestObjectResult("Invalid request payload");

            OrchestratorRequest req = JsonConvert.DeserializeObject<OrchestratorRequest>(payload.ToString());

            if(!string.IsNullOrEmpty(req.TargetPlatform))
            {
                switch (req.TargetPlatform)
                {
                    case "IoTHubServer":
                        JObject message = JObject.Parse(payload.ToString());
                        //Here i'm assuming the payload doesn't include the deviceId, adding it here:
                        if (!message.ContainsKey(EntityIdAttributeName))
                            message.Add(EntityIdAttributeName, entityId);
                        //var messageJson = message.ToString();
                        var messageJson = JsonConvert.SerializeObject(message);
                        await daprClient.PublishEventAsync<string>(serverOptions.ServiceBusName, serverOptions.ServiceBusTopic, messageJson);
                        break;
                    //case "AnotherTargetSystem":
                        //TODO: add business logic to handle publishing to the relevant bus
                    default:
                        throw new ArgumentException("Input invalid");
                }
            }
            
            
            return (ActionResult)new OkResult();
        }
    }
}
