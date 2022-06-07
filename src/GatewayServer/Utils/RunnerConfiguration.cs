using Microsoft.Extensions.Caching.Memory;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GatewayServer.Utils
{
    public class RunnerConfiguration
    {

        public string? IotHubConnectionString { get; set; }
        public bool IsCacheEnabled { get; set; }
        public string? TestDeviceId { get; set; }
        public int CacheExpireationWindowSeconds { get; set; }
        public MemoryCacheEntryOptions? CacheOptions { get; private set; }
        public bool IsSuccessLogsEnabled { get; set; } = false;
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
            config.IsCacheEnabled = configuration.GetValue<bool?>(nameof(IsCacheEnabled))?? false;
            config.TestDeviceId = configuration.GetValue<string>(nameof(TestDeviceId));
            config.CacheExpireationWindowSeconds = configuration.GetValue<int?>(nameof(CacheExpireationWindowSeconds))?? 600;
            config.CacheOptions = new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(config.CacheExpireationWindowSeconds)
                //SlidingExpiration = TimeSpan.FromSeconds(config.CacheExpireationWindowSeconds)
            };
            config.CacheOptions.RegisterPostEvictionCallback(RunnerStatusManager.OnPostEviction);
            config.IsSuccessLogsEnabled = configuration.GetValue<bool?>(nameof(IsSuccessLogsEnabled)) ?? false;
            return config;
        }
    }
}
