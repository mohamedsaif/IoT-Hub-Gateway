using GatewayServer.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GatewayServer.Models
{
    public interface IDeviceFactory
    {
        CloudDevice Create(string deviceId, RunnerConfiguration config);
    }
}
