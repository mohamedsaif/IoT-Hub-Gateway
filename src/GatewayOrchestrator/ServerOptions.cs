using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GatewayOrchestrator
{
    public class ServerOptions
    {
        // <summary>
        /// Indicates the current used version and is displayed in the health endpoint
        /// </summary>
        public string AppVersion { get; set; }

        /// <summary>
        /// The service bus name that will be used by dapr
        /// </summary>
        public string ServiceBusName { get; set; }

        /// <summary>
        /// The service bus topic that will be used by dapr to publish messages to
        /// </summary>
        public string ServiceBusTopic { get; set; }

        /// <summary>
        /// The entity id json path
        /// </summary>
        public string EntityIdAttributeName { get; set; }

        /// <summary>
        /// Flag to indicate if successful operations should be logged
        /// </summary>
        public bool IsSuccessLogsEnabled { get; set; } = false;
    }
}
