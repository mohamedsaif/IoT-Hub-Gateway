using Dapr.Client;
using GatewayServer.Services;
using GatewayServer.Utils;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GatewayServer.Controllers
{
    [Route("api/[controller]")]
    public class GatewayController : Controller
    {
        private RunnerConfiguration runnerConfigs;
        private DaprClient daprClient;
        private ILogger<GatewayController> logger;
        private IMemoryCache cache;

        public GatewayController(RunnerConfiguration runnerConfigs, DaprClient daprClient, ILogger<GatewayController> logger, IMemoryCache cache)
        {
            this.runnerConfigs = runnerConfigs;
            this.daprClient = daprClient;
            this.logger = logger;
            this.cache = cache;
        }

        [HttpGet]
        [Route("version")]
        public async Task<IActionResult> Get()
        {
            return await Task.FromResult<IActionResult>(new ObjectResult(new { Version = "2.0", Cache = runnerConfigs.IsCacheEnabled }));
        }

        [HttpGet]
        [Route("status")]
        public async Task<IActionResult> GetStatus()
        {
            return await Task.FromResult<IActionResult>(new ObjectResult(RunnerStatusManager.RunnerSavedState));
        }

        /// <summary>
        /// Sends a message for the given device
        /// </summary>
        /// <param name="deviceId">Device identifier</param>
        /// <param name="payload">Payload (JSON format)</param>
        /// <returns></returns>
        [HttpPost("{deviceId}")]
        public async Task<IActionResult> Send(string deviceId, [FromBody] dynamic payload)
        {
            try
            {
                if (string.IsNullOrEmpty(deviceId))
                {
                    logger.LogWarning("Gateway ERROR: No device id");
                    return BadRequest(new { error = "Missing deviceId" });
                }

                if (payload is null)
                {
                    logger.LogWarning("Gateway ERROR: No payload");
                    return BadRequest(new { error = "Missing payload" });
                }

                //logger.LogInformation($"Gateway: started for {deviceId}");
                var deviceFactory = new DeviceFactory(deviceId, runnerConfigs, daprClient, cache);
                await deviceFactory.Device.Sender.SendMessageAsync(payload.ToString(), CancellationToken.None);
                logger.LogInformation($"Gateway SUCCESS: Device ({deviceId}) message sent");
                return Ok();
            }
            catch (Exception ex)
            {
                logger.LogError($"Gateway ERROR: for ({deviceId}) - Message ({ex.Message}) || {ex.StackTrace}");
                return BadRequest(new { Error = ex.Message, Stack = ex.StackTrace });
                //return new StatusCodeResult(500);
            }
        }
    }
}
