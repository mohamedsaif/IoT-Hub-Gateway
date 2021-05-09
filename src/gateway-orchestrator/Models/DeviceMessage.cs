using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GatewayOrchestrator.Models
{
    public class DeviceMessage
    {
        public string DeviceId { get; set; }
        public string Payload { get; set; }
    }
}
