﻿using Dapr;
using Dapr.Client;
using GatewayTranslator.Utils;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
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
    [Route("[controller]")]
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
            return Task.FromResult(HealthCheckResult.Healthy($"Service running version ({serverOptions.AppVersion})"));
        }

        [HttpGet]
        [Route("version")]
        public async Task<IActionResult> GetHealth()
        {
            return Ok($"Service is running version ({serverOptions.AppVersion})");
        }

        [HttpPost]
        [Route("iot-gateway")]
        [Topic("gateway-servicebus", "d2c-messages")]
        public async Task<ActionResult> ProcessMessage(string d2cMessage)
        {
            logger.LogInformation($"SB triggered Gateway Translator for {d2cMessage}");

            if (IsMessageValid(d2cMessage))
            {
                try
                {
                    dynamic payloadJson = JsonConvert.DeserializeObject(d2cMessage);
                    string deviceId = payloadJson?.deviceId;

                    //Remove the id as it no longer needed in the final submission to gateway (id is passed in the request)
                    payloadJson.Remove("deviceId");

                    var payloadString = JsonConvert.SerializeObject(payloadJson);
                    var payload = new StringContent(payloadString, Encoding.UTF8, "application/json");
                    var gatewayHost = serverOptions.GatewayServerHost;
                    var getewayRequestUrl = $"{gatewayHost}/{deviceId}";
                    using (var httpClient = httpClientFactory.CreateClient())
                    {
                        //var result = await httpClient.PostAsync(getewayRequestUrl, payload);

                        //if (result.StatusCode != System.Net.HttpStatusCode.OK)
                        //{
                        //    string serverResponse = await result.Content.ReadAsStringAsync();
                        //    throw new ApplicationException($"Failed to process message from IoT Hub Gateway. Payload: {d2cMessage}. ERROR: {result.StatusCode}||{serverResponse}");
                        //}
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