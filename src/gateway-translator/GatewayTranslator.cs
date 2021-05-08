using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using GatewayTranslator.Utils;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;

namespace GatewayTranslator
{
    public class GatewayTranslator
    {
        private IHttpClientFactory httpClientFactory;

        public GatewayTranslator(IHttpClientFactory httpClientFactory)
        {
            this.httpClientFactory = httpClientFactory;
        }

        [FunctionName("gateway-translator")]
        public async Task ProcessMessage(
            [ServiceBusTrigger("d2c-messages", "d2c-messages-sub", Connection = "gateway-translator-sb-conn")]string d2cMessage, 
            ILogger log)
        {
            log.LogInformation($"gateway-translator ServiceBus topic trigger function processed message: {d2cMessage}");

            if (IsMessageValid(d2cMessage))
            {
                try
                {
                    var payload = new StringContent(d2cMessage, Encoding.UTF8, "application/json");
                    var gatewayHost = GlobalSettings.GetKeyValue("gateway-server-host");
                    var httpClient = httpClientFactory.CreateClient();
                    var result = await httpClient.PostAsync(gatewayHost, payload);

                    if (result.StatusCode != System.Net.HttpStatusCode.OK)
                    {
                        throw new ApplicationException($"Failed to process message from IoT Hub Gateway. Payload: {d2cMessage}");
                    }
                }
                catch (Exception ex)
                {
                    log.LogError($"gateway-translator Failed to process message from IoT Hub Gateway. Payload: {d2cMessage}");
                    throw ex;
                }
            }
            else
            {
                //invalid messages handling here
            }
        }

        private bool IsMessageValid(string message)
        {
            //Simple validation (needs to be updated to reflect real validation
            return !string.IsNullOrEmpty(message);
        }
    }
}
