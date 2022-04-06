using Dapr;
using Dapr.Client;
using GatewayTranslator.Utils;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace GatewayTranslator.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class GatewayTranslatorController : ControllerBase, IHealthCheck
    {
        private DaprClient daprClient;
        private IHttpClientFactory httpClientFactory;
        private ILogger<GatewayTranslatorController> logger;
        private ServerOptions serverOptions;

        public GatewayTranslatorController(DaprClient dapr, IHttpClientFactory httpClientFactory, ServerOptions serverOptions, ILogger<GatewayTranslatorController> logger)
        {
            daprClient = dapr ?? throw new ArgumentNullException(nameof(dapr));
            this.httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(IHttpClientFactory));
            this.logger = logger;
            this.serverOptions = serverOptions;
        }

        [HttpGet]
        public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(HealthCheckResult.Healthy(JsonConvert.SerializeObject(
                new { Message = $"Service running version ({serverOptions.AppVersion})" }
                )));
        }

        [HttpGet]
        [Route("Version")]
        public async Task<IActionResult> GetVersion()
        {
            return Ok(new { Message = $"Service is running version ({serverOptions.AppVersion}) and Simulation Mode is ({serverOptions.SimulationMode})" });
        }

        [HttpPost]
        [Route("Process")]
        [Topic("gateway-servicebus", "d2c-messages")]
        public async Task<ActionResult> ProcessMessage(dynamic d2cMessage)
        {
            logger.LogInformation($"SB triggered Gateway Translator for {d2cMessage}");
            var d2cMessageString = d2cMessage.ToString();
            var isValidMessage = IsMessageValid(d2cMessageString);

            if (isValidMessage)
            {
                try
                {
                    JObject message = JObject.Parse(d2cMessageString);
                    var idToken = message.SelectToken(serverOptions.EntityIdAttributeName);
                    string deviceId = idToken != null ? idToken.Value<string>() : string.Empty;
                    if (string.IsNullOrEmpty(deviceId))
                        throw new ArgumentException($"Invalid payload due to no id at ({serverOptions.EntityIdAttributeName})");
                    
                    var messageJsonString = JsonConvert.SerializeObject(message);
                    var payload = new StringContent(messageJsonString, Encoding.UTF8, "application/json");
                    var gatewayHost = serverOptions.GatewayServerHost;
                    var getewayRequestUrl = $"{gatewayHost}/{deviceId}";

                    if(serverOptions.SimulationMode)
                    {
                        logger.LogInformation($"SB triggered Gateway Translator completed SIMULATION successfully for: {d2cMessage}");
                        return new OkObjectResult(new { Message = "Message simlation posted successfully" });
                    }

                    using (var httpClient = httpClientFactory.CreateClient())
                    {
                        var result = await httpClient.PostAsync(getewayRequestUrl, payload);

                        if (result.StatusCode != System.Net.HttpStatusCode.OK)
                        {
                            string serverResponse = await result.Content.ReadAsStringAsync();
                            throw new ApplicationException($"Failed to process message from IoT Hub Gateway. Payload: {d2cMessage}. ERROR: {result.StatusCode}||{serverResponse}");
                        }
                        logger.LogInformation($"SB triggered Gateway Translator completed successfully for: {d2cMessage}");
                        return new OkObjectResult("Message posted successfully");
                    }
                }
                catch (Exception ex)
                {
                    logger.LogError($"gateway-translator Failed to process message from IoT Hub Gateway. Payload: {d2cMessage} with error ({ex.Message}: {ex.StackTrace}");
                    throw;
                }
            }
            else
            {
                //invalid messages handling here
                throw new ArgumentException("Message is not in correct format");
            }
        }

        private bool IsMessageValid(string message)
        {
            //Simple validation (needs to be updated to reflect real validation
            return !string.IsNullOrEmpty(message);
        }
    }
}
