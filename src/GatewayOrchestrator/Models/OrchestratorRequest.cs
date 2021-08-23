using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GatewayOrchestrator.Models
{
    public class OrchestratorRequest
    {
        public string TargetPlatform { get; set; }
        public string SourceSystem { get; set; }
    }
}
