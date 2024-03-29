﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GatewayTranslator
{
    public class ServerOptions
    {
        /// <summary>
        /// Indicates the current used version and is displayed in the health endpoint
        /// </summary>
        public string AppVersion { get; set; }

        /// <summary>
        /// Indicates if messages processing is simulated or actual (simlation can happen without depending on IoT-Hub-Server service)
        /// </summary>
        public bool SimulationMode { get; set; }

        /// <summary>
        /// Url to iot-hub gateway server to post the messages to
        /// </summary>
        public string GatewayServerHost { get; set; }

        public string GatewayServerHealthEndpoint { get; set; }

        /// <summary>
        /// The service bus name that will be used by dapr
        /// </summary>
        public string ServiceBusName { get; set; }

        /// <summary>
        /// The service bus topic that will be used by dapr to publish messages to
        /// </summary>
        public string ServiceBusTopic { get; set; }

        /// <summary>
        /// Set the location of the entity id in the target json payload
        /// </summary>
        public string EntityIdAttributeName { get; set; } = "id";

        /// <summary>
        /// Flag to indicate if successful operations should be logged
        /// </summary>
        public bool IsSuccessLogsEnabled { get; set; } = false;

    }
}
