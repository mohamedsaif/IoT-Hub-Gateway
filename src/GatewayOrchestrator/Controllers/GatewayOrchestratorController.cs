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

        [HttpGet]
        [Route("version")]
        public async Task<IActionResult> GetVersion()
        {
            return await Task.FromResult<IActionResult>(new ObjectResult(new { Version = serverOptions.AppVersion, EntityIdPath = serverOptions.EntityIdAttributeName }));
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
            if (string.IsNullOrEmpty(targetPlatform))
                return (ActionResult)new BadRequestObjectResult("Orchestrator ERROR: Invalid request parameters");

            if (payload is null)
                return (ActionResult)new BadRequestObjectResult("Orchestrator ERROR: Invalid request payload");

            
            switch (targetPlatform)
            {
                case "IoTHubServer":
                    JObject message = JObject.Parse(payload.ToString());
                    // Validate that the payload include the defined EntityIdAttributeName
                    var idToken = message.SelectToken(serverOptions.EntityIdAttributeName);
                    string deviceId = idToken != null ? idToken.Value<string>() : string.Empty;
                    if (string.IsNullOrEmpty(deviceId))
                        throw new ArgumentException($"Orchestrator ERROR: Invalid payload due to no id at ({serverOptions.EntityIdAttributeName})");
                    try
                    {
                        var messageJson = JsonConvert.SerializeObject(message);
                        await daprClient.PublishEventAsync<string>(serverOptions.ServiceBusName, serverOptions.ServiceBusTopic, messageJson);
                        if (serverOptions.IsSuccessLogsEnabled)
                            logger.LogInformation($"Orchestrator SUCCESS: Proccessed device ({deviceId}) message");
                    }
                    catch (Exception ex)
                    {
                        logger.LogError($"Orchestrator ERROR: failed to ingest message for ({deviceId}) due to ({ex.Message})");
                        return (ActionResult)new UnprocessableEntityObjectResult("Orchestrator ERROR: failed to ingest message");
                    }


                    
                    break;
                //case "AnotherTargetSystem":
                    //TODO: add business logic to handle publishing to the relevant bus
                default:
                    throw new ArgumentException("Orchestrator ERROR: Input target platform");
            }
            
            
            
            return (ActionResult)new OkResult();
        }
    }
}
