using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using GatewayTranslator.Utils;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace GatewayTranslator
{
    public class GatewayTranslator
    {
        private IHttpClientFactory httpClientFactory;

        public GatewayTranslator(IHttpClientFactory httpClientFactory)
        {
            this.httpClientFactory = httpClientFactory;
        }

        [FunctionName("GatewayTranslator")]
        public async Task ProcessMessage(
            [ServiceBusTrigger("d2c-messages", "d2c-messages-sub", Connection = "gateway-translator-sb-conn")]string d2cMessage, 
            ILogger log)
        {
            log.LogInformation($"gateway-translator ServiceBus topic trigger function processed message: {d2cMessage}");

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
                    var gatewayHost = GlobalSettings.GetKeyValue("gateway-server-host");
                    var getewayRequestUrl = $"{gatewayHost}/{deviceId}";
                    var httpClient = httpClientFactory.CreateClient();
                    var result = await httpClient.PostAsync(getewayRequestUrl, payload);

                    if (result.StatusCode != System.Net.HttpStatusCode.OK)
                    {
                        string serverResponse = await result.Content.ReadAsStringAsync();
                        throw new ApplicationException($"Failed to process message from IoT Hub Gateway. Payload: {d2cMessage}. ERROR: {result.StatusCode}||{serverResponse}");
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
