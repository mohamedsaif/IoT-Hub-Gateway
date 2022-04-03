using Dapr.Client;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IoTGatewayServer.Controllers
{
    public class GatewayController : ControllerBase
    {
        private readonly DaprClient daprClient;
        private readonly ILogger<WeatherForecastController> logger;

        public GatewayController(DaprClient daprClient, ILogger<WeatherForecastController> logger)
        {
            this.daprClient = daprClient;
            this.logger = logger;
        }
    }
}
