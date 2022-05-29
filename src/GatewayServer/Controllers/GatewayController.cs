using Dapr.Client;
using GatewayServer.Models;
using GatewayServer.Services;
using GatewayServer.Utils;
using Microsoft.AspNetCore.Mvc;
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
        private RunnerStats runnerStats;
        private ILogger<GatewayController> logger;

        public GatewayController(RunnerConfiguration runnerConfigs, RunnerStats stats, DaprClient daprClient, ILogger<GatewayController> logger)
        {
            this.runnerConfigs = runnerConfigs;
            this.daprClient = daprClient;
            this.runnerStats = stats;
            this.logger = logger;
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
            return await Task.FromResult<IActionResult>(new ObjectResult(runnerStats));
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
                    return BadRequest(new { error = "Missing deviceId" });

                if (payload is null)
                    return BadRequest(new { error = "Missing payload" });

                var deviceFactory = new DeviceFactory(deviceId, runnerConfigs, daprClient, runnerStats);
                await deviceFactory.Device.Sender.SendMessageAsync(payload.ToString(), runnerStats, CancellationToken.None);

                return Ok();
            }
            catch (Exception ex)
            {
                return BadRequest(new { Error = ex.Message, Stack = ex.StackTrace });
                //return new StatusCodeResult(500);
            }
        }
    }
}
