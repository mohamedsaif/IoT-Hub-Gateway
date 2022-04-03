using GatewayServer.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GatewayServer.Models
{
    public class CloudDevice
    {
        public string DeviceId { get; private set; }
        private RunnerConfiguration config;
        public ISender Sender { get; private set; }

        public CloudDevice(string deviceId, RunnerConfiguration config, ISender sender)
        {
            this.DeviceId = deviceId;
            this.config = config;
            this.Sender = sender;
        }
    }
}
