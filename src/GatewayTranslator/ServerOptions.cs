using System;
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
        /// Url to iot-hub gateway server to post the messages to
        /// </summary>
        public string GatewayServerHost { get; set; }
    }
}
