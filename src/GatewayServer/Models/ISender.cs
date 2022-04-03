using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GatewayServer.Models
{
    public interface ISender
    {
        Task OpenAsync();

        Task SendMessageAsync(string message, RunnerStats stats, CancellationToken cancellationToken);
    }
}
