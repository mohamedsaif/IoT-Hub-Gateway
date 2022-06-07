using Dapr.Client;
using GatewayServer.Models;
using GatewayServer.Utils;
using Microsoft.Azure.Devices.Client;
using Microsoft.Extensions.Caching.Memory;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GatewayServer.Services
{
    public class DeviceFactory
    {
        private DaprClient daprClient;
        private const string DAPR_STORE_NAME = "devicestore";

        public CloudDevice Device { get; }

        private IMemoryCache cache;

        public DeviceFactory(string deviceId, RunnerConfiguration config, DaprClient daprClient, IMemoryCache cache)
        {
            this.daprClient = daprClient;
            this.cache = cache;
            Device = (this.Create(deviceId, config)).Result;
        }

        public async Task<CloudDevice> Create(string deviceId, RunnerConfiguration config)
        {
            var sender = await this.GetSender(deviceId, config);
            return new CloudDevice(deviceId, config, sender);
        }

        private async Task<ISender> GetSender(string deviceId, RunnerConfiguration config)
        {
            if (!string.IsNullOrEmpty(config.IotHubConnectionString))
            {
                return await GetIotHubSender(deviceId, config);
            }

            throw new ArgumentException("No connnection string specified");
        }

        private async Task<ISender> GetIotHubSender(string deviceId, RunnerConfiguration config)
        {
            DeviceClient? device = null;
            //Check if cache is enabled
            if (config.IsCacheEnabled)
            {
                device = cache.Get<DeviceClient>(deviceId);
            }

            if (device == null)
            {
                // create one deviceClient for each device
                device = DeviceClient.CreateFromConnectionString(
                    config.IotHubConnectionString,
                    deviceId,
                    new ITransportSettings[]
                    {
                        new AmqpTransportSettings(Microsoft.Azure.Devices.Client.TransportType.Amqp_Tcp_Only)
                        {
                            AmqpConnectionPoolSettings = new AmqpConnectionPoolSettings()
                            {
                                Pooling = true,
                            }
                        }
                    });

                RunnerStatusManager.IncrementDeviceConnected();

                //Cache the device
                if(config.IsCacheEnabled)
                {
                    cache.Set<DeviceClient>(deviceId, device, config.CacheOptions);
                    Console.WriteLine($"Gateway CACHE: added to cache ({deviceId})");
                }
            }
            else
            {
                //Console.WriteLine($"Gateway CACHE: loaded from cache ({deviceId})");
            }

            return new IoTHubSender(device, deviceId, config);
        }
    }
}
