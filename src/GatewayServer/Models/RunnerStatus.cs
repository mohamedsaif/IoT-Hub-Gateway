using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GatewayServer.Models
{
    public class RunnerStatus
    {
        public long connectedDevices;
        public long completedDevices;
        public long messagesSent;
        public long totalSendTelemetryErrors;
        public long totalSendTelemetryTransientErrors;

        public long messagesSendingStart;
    }
}
