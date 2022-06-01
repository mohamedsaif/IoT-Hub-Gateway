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

        public long ConnectedDevices { 
            get { return Interlocked.Read(ref connectedDevices); }
        }
        public long CompletedDevices { 
            get { return Interlocked.Read(ref completedDevices); }
        }
        public long MessagesSent { 
            get { return Interlocked.Read(ref messagesSent); }
        }
        public long TotalSendTelemetryErrors {
            get { return Interlocked.Read(ref totalSendTelemetryErrors); }
        }
        public long TotalSendTelemetryTransientErrors {
            get { return Interlocked.Read(ref totalSendTelemetryTransientErrors); }
        }

        public DateTime MessagesSendingStartUTC {
            get { return new DateTime(Interlocked.Read(ref messagesSendingStart)); }
        }
    }
}
