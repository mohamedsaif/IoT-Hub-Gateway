using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GatewayServer.Utils
{
    public class RunnerConfiguration
    {

        public string IotHubConnectionString { get; set; }
        public bool IsCacheEnabled { get; set; }

        public void EnsureIsValid()
        {
            var numberOfConnectionSettings = 0;
            if (!string.IsNullOrWhiteSpace(this.IotHubConnectionString))
                numberOfConnectionSettings++;
            
            if (numberOfConnectionSettings != 1)
            {
                throw new Exception(
                    $"Exactly one of {nameof(this.IotHubConnectionString)} must be defined");
            }
        }

        public static RunnerConfiguration Load(IConfiguration configuration)
        {
            var config = new RunnerConfiguration();
            config.IotHubConnectionString = configuration.GetValue<string>(nameof(IotHubConnectionString));
            config.IsCacheEnabled = configuration.GetValue<bool>(nameof(IsCacheEnabled));

            return config;
        }
    }
}
