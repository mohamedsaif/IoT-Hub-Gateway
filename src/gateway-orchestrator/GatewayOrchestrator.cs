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

namespace gateway_orchestrator
{
    public static class GatewayOrchestrator
    {
        [FunctionName("GatewayOrchestrator")]
        [OpenApiOperation(operationId: "Run", tags: new[] { "name" })]
        [OpenApiSecurity("function_key", SecuritySchemeType.ApiKey, Name = "code", In = OpenApiSecurityLocationType.Query)]
        [OpenApiParameter(name: "name", In = ParameterLocation.Query, Required = true, Type = typeof(string), Description = "The **Name** parameter")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "text/plain", bodyType: typeof(string), Description = "The OK response")]
        //[return: ServiceBus("d2c-messages", Connection = "gateway-orchestrator-sb-conn", EntityType = Microsoft.Azure.WebJobs.ServiceBus.EntityType.Topic)]
        public static IActionResult Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req,
             [ServiceBus("d2c-messages", Connection = "gateway-orchestrator-sb-conn", EntityType = EntityType.Topic)] ICollector<string> topicCollector,
            ILogger log)
        {
            log.LogInformation("GatewayOrchestrator: HTTP trigger function processed a request.");

            string requestBody = new StreamReader(req.Body).ReadToEnd();
            dynamic data = JsonConvert.DeserializeObject(requestBody);

            string d2cPayload = data?.d2cPayload;
            if (string.IsNullOrEmpty(d2cPayload))
                return (ActionResult)new BadRequestObjectResult("Invalid request data");
            
            topicCollector.Add(d2cPayload);
            return (ActionResult)new OkResult();
        }
    }
}

