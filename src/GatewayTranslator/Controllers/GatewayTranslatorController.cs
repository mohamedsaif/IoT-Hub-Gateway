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
    public class GatewayTranslatorController : ControllerBase
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
        [Route("Version")]
        public async Task<IActionResult> GetVersion()
        {
            return await Task.FromResult<IActionResult>(Ok(new { Message = $"Service is running version ({serverOptions.AppVersion}) and Simulation Mode is ({serverOptions.SimulationMode})" }));
        }

        [HttpPost]
        [Route("Process")]
        [Topic("gateway-servicebus", "d2c-messages")]
        public async Task<ActionResult> ProcessMessage(dynamic d2cMessage)
        {
            var d2cMessageString = d2cMessage.ToString();
            var isValidMessage = IsMessageValid(d2cMessageString);
            string deviceId = null;

            if (isValidMessage)
            {
                try
                {
                    JObject message = JObject.Parse(d2cMessageString);
                    var idToken = message.SelectToken(serverOptions.EntityIdAttributeName);
                    deviceId = idToken != null ? idToken.Value<string>() : string.Empty;
                    if (string.IsNullOrEmpty(deviceId))
                    {
                        logger.LogError($"Translator ERROR: Missing device id ({deviceId})");
                        throw new ArgumentException($"Invalid device due to no id at ({serverOptions.EntityIdAttributeName})");
                    }

                    //logger.LogInformation($"Translator: started for {deviceId}");
                    var messageJsonString = JsonConvert.SerializeObject(message);
                    var payload = new StringContent(messageJsonString, Encoding.UTF8, "application/json");
                    var gatewayHost = serverOptions.GatewayServerHost;
                    var getewayRequestUrl = $"{gatewayHost}/{deviceId}";

                    if(serverOptions.SimulationMode)
                    {
                        if(serverOptions.IsSuccessLogsEnabled)
                            logger.LogInformation($"Translator SUCCESS: completed SIMULATION successfully for: {deviceId}");
                        return new OkObjectResult(new { Message = $"Message simlation posted successfully for: {deviceId}" });
                    }

                    using (var httpClient = httpClientFactory.CreateClient())
                    {
                        var result = await httpClient.PostAsync(getewayRequestUrl, payload);

                        if (result.StatusCode != System.Net.HttpStatusCode.OK)
                        {
                            string serverResponse = await result.Content.ReadAsStringAsync();
                            logger.LogError($"Translator ERROR: failed to post message to Server for ({deviceId})");
                            throw new ApplicationException($"Failed to process message from IoT Hub Gateway. ERROR: {result.StatusCode}||{serverResponse}");
                        }

                        if (serverOptions.IsSuccessLogsEnabled)
                            logger.LogInformation($"Translator SUCCESS: completed successfully for: {deviceId}");
                        return new OkObjectResult("Message posted successfully");
                    }
                }
                catch (Exception ex)
                {
                    logger.LogError($"Translator ERROR: IoT Hub Gateway Server call failed for Device ({deviceId}) with error ({ex.Message}||{ex.StackTrace}");
                    throw;
                }
            }
            else
            {
                //invalid messages handling here
                logger.LogError($"Translator ERROR: Incorrect format");
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
