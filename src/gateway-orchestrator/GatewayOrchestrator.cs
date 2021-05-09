using System.IO;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Enums;
using Microsoft.Azure.WebJobs.ServiceBus;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
using Newtonsoft.Json;

namespace GatewayOrchestrator
{
    public class GatewayOrchestrator
    {
        [FunctionName("GatewayOrchestrator")]
        [OpenApiOperation(operationId: "Run", tags: new[] { "name" })]
        [OpenApiSecurity("function_key", SecuritySchemeType.ApiKey, Name = "code", In = OpenApiSecurityLocationType.Query)]
        [OpenApiParameter(name: "name", In = ParameterLocation.Query, Required = true, Type = typeof(string), Description = "The **Name** parameter")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(string), Description = "The OK response")]
        //[return: ServiceBus("d2c-messages", Connection = "gateway-orchestrator-sb-conn", EntityType = Microsoft.Azure.WebJobs.ServiceBus.EntityType.Topic)]
        public async Task<IActionResult> Run(
             [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = null)] HttpRequest req,
             [ServiceBus("d2c-messages", Connection = "gateway-orchestrator-sb-conn", EntityType = EntityType.Topic)] ICollector<dynamic> topicCollector,
            ILogger log)
        {
            log.LogInformation("GatewayOrchestrator: HTTP trigger function processed a request.");

            //get device id
            string deviceId = req.Query["deviceId"];
            if(string.IsNullOrEmpty(deviceId))
                return (ActionResult)new BadRequestObjectResult("Invalid request parameters");

            string requestBody = new StreamReader(req.Body).ReadToEnd();
            if (string.IsNullOrEmpty(requestBody))
                return (ActionResult)new BadRequestObjectResult("Invalid request data");
            
            dynamic data = JsonConvert.DeserializeObject(requestBody);
            //Here i'm assuming the payload doesn't include the deviceId, adding it here:
            data.deviceId = deviceId;
            
            topicCollector.Add(data);
            return (ActionResult)new OkResult();
        }
    }
}

