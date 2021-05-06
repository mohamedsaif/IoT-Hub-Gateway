using System;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;

namespace gateway_translator
{
    public static class GatewayTranslator
    {
        [FunctionName("gateway-translator")]
        public static void Run(
            [ServiceBusTrigger("d2c-messages", "d2c-messages-sub", Connection = "gateway-translator-sb-conn")]string d2cMessage, 
            ILogger log)
        {
            log.LogInformation($"gateway-translator ServiceBus topic trigger function processed message: {d2cMessage}");

        }
    }
}
