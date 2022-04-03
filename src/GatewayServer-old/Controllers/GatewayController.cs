using GatewayServer.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System;
using System.Threading.Tasks;

namespace GatewayServer.Controllers
{
    [Route("iot-gateway")]
    public class GatewayController : Controller
    {
        private readonly IGatewayService gatewayService;
        private readonly ServerOptions options;
        private static readonly DateTime epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        public GatewayController(IGatewayService gatewayService, ServerOptions options)
        {
            this.gatewayService = gatewayService;
            this.options = options;
        }

        [HttpGet]
        public async Task<IActionResult> Health()
        {
            return Ok($"Service version ({options.AppVersion}) is running... and IoT Hub SDK (v1.7.2)");
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

                var sasToken = this.ControllerContext.HttpContext.Request.Headers[Constants.SasTokenHeaderName].ToString();
                if (!string.IsNullOrEmpty(sasToken))
                {
                    var tokenExpirationDate = ResolveTokenExpiration(sasToken);
                    if (!tokenExpirationDate.HasValue)
                        tokenExpirationDate = DateTime.UtcNow.AddMinutes(20);

                    await gatewayService.SendDeviceToCloudMessageByToken(deviceId, payload.ToString(), sasToken, tokenExpirationDate.Value);
                }
                else
                {
                    if (!this.options.SharedAccessPolicyKeyEnabled)
                        return BadRequest(new { error = "Shared access is not enabled" });
                    await gatewayService.SendDeviceToCloudMessageBySharedAccess(deviceId, payload.ToString());
                }

                return Ok();
            }
            catch (Exception ex)
            {
                return BadRequest(new { Error = ex.Message, Stack = ex.StackTrace });
                //return new StatusCodeResult(500);
            }
        }

        /// <summary>
        /// Expirations is available as parameter "se" as a unix time in our sample application
        /// </summary>
        /// <param name="sasToken"></param>
        /// <returns></returns>
        private DateTime? ResolveTokenExpiration(string sasToken)
        {
            // TODO: Implement in more reliable way (regex or another built-in class)
            const string field = "se=";
            var index = sasToken.LastIndexOf(field);
            if (index >= 0)
            {
                var unixTime = sasToken.Substring(index + field.Length);
                if (int.TryParse(unixTime, out var unixTimeInt))
                {
                    return epoch.AddSeconds(unixTimeInt);
                }
            }

            return null;
        }
    }
}
