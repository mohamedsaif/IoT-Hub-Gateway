using Dapr.Client;
using GatewayServer.Models;
using GatewayServer.Utils;
using Microsoft.Azure.Devices.Client;
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
        private RunnerStats runnerStats;
        private const string DAPR_STORE_NAME = "devicestore";

        public CloudDevice Device { get; }

        public DeviceFactory(string deviceId, RunnerConfiguration config, DaprClient daprClient, RunnerStats runnerStats)
        {
            this.daprClient = daprClient;
            this.runnerStats = runnerStats;
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
                var deviceCache = await daprClient.GetStateEntryAsync<DeviceClient>(DAPR_STORE_NAME, deviceId);
                if(deviceCache != null)
                {
                    device = deviceCache.Value;
                }
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

                runnerStats.IncrementDeviceConnected();

                //Cache the device
                if(config.IsCacheEnabled)
                {
                    await daprClient.SaveStateAsync<DeviceClient>(DAPR_STORE_NAME, deviceId, device);
                }
            }

            return new IotHubSender(device, deviceId, config);
        }
    }
}
