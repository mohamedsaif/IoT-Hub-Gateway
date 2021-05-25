
using Microsoft.Azure.Devices.Client;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;

namespace IoTHubGateway.Server.Services
{
    /// <summary>
    /// IoT Hub Device client multiplexer based on AMQP
    /// </summary>
    public class GatewayService : IGatewayService
    {
        private readonly ServerOptions serverOptions;
        private readonly IMemoryCache cache;
        private readonly ILogger<GatewayService> logger;
        RegisteredDevices registeredDevices;

        /// <summary>
        /// Sliding expiration for each device client connection
        /// Default: 30 minutes
        /// </summary>
        public TimeSpan DeviceConnectionCacheSlidingExpiration { get; set; }

        /// <summary>
        /// Constructor
        /// </summary>
        public GatewayService(ServerOptions serverOptions, IMemoryCache cache, ILogger<GatewayService> logger, RegisteredDevices registeredDevices)
        {
            this.serverOptions = serverOptions;
            this.cache = cache;
            this.logger = logger;
            this.registeredDevices = registeredDevices;
            this.DeviceConnectionCacheSlidingExpiration = TimeSpan.FromMinutes(serverOptions.DefaultDeviceCacheInMinutes);
        }

        public async Task SendDeviceToCloudMessageByToken(string deviceId, string payload, string sasToken, DateTime tokenExpiration)
        {
            var deviceClient = await ResolveDeviceClient(deviceId, sasToken, tokenExpiration);
            if (deviceClient == null)
            {
                if (this.serverOptions.CreateDevices)
                {
                    try
                    {
                        await ProvisionDevice(deviceId);
                        deviceClient = await ResolveDeviceClient(deviceId);
                    }
                    catch (Exception ex)
                    {
                        this.logger.LogError($"Failed to create device with error: {ex.Message}");
                        throw ex;
                    }
                }
                //else
                //{
                //    throw new DeviceConnectionException($"Failed to connect to device {deviceId}");
                //}
            }

            try
            {
                await deviceClient.SendEventAsync(new Message(Encoding.UTF8.GetBytes(payload))
                {
                    ContentEncoding = "utf-8",
                    ContentType = "application/json"
                });

                this.logger.LogInformation($"Event sent to device {deviceId} using device token. Payload: {payload}");
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, $"Could not send device message to IoT Hub (device: {deviceId}) with error ({ex.Message}) and stack: {ex.StackTrace}");
                throw;
            }

        }

        public async Task SendDeviceToCloudMessageBySharedAccess(string deviceId, string payload)
        {
            var deviceClient = await ResolveDeviceClient(deviceId);
            if(deviceClient == null)
            {
                if (this.serverOptions.CreateDevices)
                {
                    try
                    {
                        for (int i = 1; i <= 3; i++)
                        {
                            await ProvisionDevice(deviceId);
                            deviceClient = await ResolveDeviceClient(deviceId);
                            if (deviceClient != null)
                                break;
                            else
                            {
                                this.logger.LogError($"Failed to provision new device with error in trial number ({i}). Sleeping for 15 seconds");
                                Thread.Sleep(15000);
                            }
                        }
                        //If creating the device still didn't fix the issue, throw an exception
                        if (deviceClient == null)
                            throw new DeviceConnectionException($"Provisioning a device didn't fix the device error for device: {deviceId}");
                    }
                    catch (Exception ex)
                    {
                        this.logger.LogError($"Failed to create device with error: {ex.Message}, stack: {ex.StackTrace}");
                        throw ex;
                    }
                }
                //else
                //{
                //    throw new DeviceConnectionException($"Failed to connect to device {deviceId}");
                //}
            }

            if (deviceClient == null)
                throw new NullReferenceException($"Device is still null and can't be resolved for deviceId ({deviceId}) and payload: {payload}");

            try
            { 
                await deviceClient.SendEventAsync(new Message(Encoding.UTF8.GetBytes(payload))
                {
                    ContentEncoding = "utf-8",
                    ContentType = "application/json"
                });

                this.logger.LogInformation($"Event sent to device {deviceId} using shared access. Payload: {payload}");
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, $"Could not send device message to IoT Hub (device: {deviceId})");
                throw;
            }

        }

        private async Task<DeviceClient> ResolveDeviceClient(string deviceId, string sasToken = null, DateTime? tokenExpiration = null)
        {
            DeviceClient newDeviceClient = null;
            try
            {
                if (serverOptions.IsCacheDisabled)
                {
                    IAuthenticationMethod auth = null;
                    if (string.IsNullOrEmpty(sasToken))
                    {
                        auth = new DeviceAuthenticationWithSharedAccessPolicyKey(deviceId, this.serverOptions.AccessPolicyName, this.serverOptions.AccessPolicyKey);
                    }
                    else
                    {
                        auth = new DeviceAuthenticationWithToken(deviceId, sasToken);
                    }

                    newDeviceClient = DeviceClient.Create(
                       this.serverOptions.IoTHubHostName,
                        auth,
                        new ITransportSettings[]
                        {
                            new AmqpTransportSettings(TransportType.Amqp_Tcp_Only)
                            {
                                AmqpConnectionPoolSettings = new AmqpConnectionPoolSettings()
                                {
                                    Pooling = true,
                                    MaxPoolSize = (uint)this.serverOptions.MaxPoolSize
                                },
                                //IdleTimeout = TimeSpan.FromMinutes(5)
                            }
                        }
                    );

                    newDeviceClient.OperationTimeoutInMilliseconds = (uint)this.serverOptions.DeviceOperationTimeout;
                    newDeviceClient.SetRetryPolicy(new ExponentialBackoff(int.MaxValue, TimeSpan.FromMilliseconds(100), TimeSpan.FromSeconds(10), TimeSpan.FromMilliseconds(100)));
                    await newDeviceClient.OpenAsync();

                    if (this.serverOptions.DirectMethodEnabled)
                        await newDeviceClient.SetMethodDefaultHandlerAsync(this.serverOptions.DirectMethodCallback, deviceId);

                    this.logger.LogInformation($"NO-CACHE connection to device {deviceId} has been established");


                    registeredDevices.AddDevice(deviceId);

                    return newDeviceClient;
                }
                else
                {
                    var deviceClient = await cache.GetOrCreateAsync<DeviceClient>(deviceId, async (cacheEntry) =>
                    {
                        IAuthenticationMethod auth = null;
                        if (string.IsNullOrEmpty(sasToken))
                        {
                            auth = new DeviceAuthenticationWithSharedAccessPolicyKey(deviceId, this.serverOptions.AccessPolicyName, this.serverOptions.AccessPolicyKey);
                        }
                        else
                        {
                            auth = new DeviceAuthenticationWithToken(deviceId, sasToken);
                        }

                        newDeviceClient = DeviceClient.Create(
                           this.serverOptions.IoTHubHostName,
                            auth,
                            new ITransportSettings[]
                            {
                            new AmqpTransportSettings(TransportType.Amqp_Tcp_Only)
                            {
                                AmqpConnectionPoolSettings = new AmqpConnectionPoolSettings()
                                {
                                    Pooling = true,
                                    MaxPoolSize = (uint)this.serverOptions.MaxPoolSize
                                },
                                //IdleTimeout = TimeSpan.FromMinutes(5)
                            }
                            }
                        );

                        newDeviceClient.OperationTimeoutInMilliseconds = (uint)this.serverOptions.DeviceOperationTimeout;
                        newDeviceClient.SetRetryPolicy(new ExponentialBackoff(int.MaxValue, TimeSpan.FromMilliseconds(100), TimeSpan.FromSeconds(10), TimeSpan.FromMilliseconds(100)));
                        await newDeviceClient.OpenAsync();

                        if (this.serverOptions.DirectMethodEnabled)
                            await newDeviceClient.SetMethodDefaultHandlerAsync(this.serverOptions.DirectMethodCallback, deviceId);

                        if (!tokenExpiration.HasValue)
                            tokenExpiration = DateTime.UtcNow.AddMinutes(this.serverOptions.DefaultDeviceCacheInMinutes);
                        cacheEntry.SetAbsoluteExpiration(tokenExpiration.Value);
                        cacheEntry.RegisterPostEvictionCallback(this.CacheEntryRemoved, deviceId);

                        this.logger.LogInformation($"Connection to device {deviceId} has been established, valid until {tokenExpiration.Value.ToString()}");


                        registeredDevices.AddDevice(deviceId);

                        return newDeviceClient;
                    });
                    return deviceClient;
                }
            }
            catch (Exception ex)
            {
                if(ex.Message.Contains("Transient network error occurred"))
                {
                    try
                    {
                        await newDeviceClient.CloseAsync();
                        Thread.Sleep(10000);
                        await newDeviceClient.OpenAsync();
                    }
                    catch (Exception innerEx)
                    {
                        this.logger.LogError(ex, $"CLOSING FAILED: Could not connect device {deviceId}, cache status ({serverOptions.IsCacheDisabled}) with error {ex.Message} and stack: {ex.StackTrace}. NEW EXP: {innerEx.Message}");
                    }
                }
                this.logger.LogError(ex, $"DEVICE-RESOLVE: Could not connect device {deviceId}, cache status ({serverOptions.IsCacheDisabled}) with error {ex.Message} and stack: {ex.StackTrace}");
                //throw new DeviceConnectionException($"Could not connect device {deviceId} with error {ex.Message}");
            }

            return null;
        }

        private void CacheEntryRemoved(object key, object value, EvictionReason reason, object state)
        {
            this.registeredDevices.RemoveDevice(key);
        }

        private async Task ProvisionDevice(string deviceId)
        {
            string iotHubConnection = $"HostName={this.serverOptions.IoTHubHostName};SharedAccessKeyName={this.serverOptions.AccessPolicyName};SharedAccessKey={this.serverOptions.AccessPolicyKey}";
            logger.LogInformation($"Initialized for registration Id {deviceId}.");

            try
            {
                logger.LogInformation("Registering with the device provisioning service...");
                Microsoft.Azure.Devices.RegistryManager rm = Microsoft.Azure.Devices.RegistryManager.CreateFromConnectionString(iotHubConnection);
                var createdDevice = await rm.AddDeviceAsync(new Microsoft.Azure.Devices.Device(deviceId) { Status = Microsoft.Azure.Devices.DeviceStatus.Enabled });
                
                logger.LogInformation($"Created device status: {createdDevice.Status}.");
            }
            catch (Exception ex)
            {
                logger.LogError($"Device provisioning error: {ex.Message}, Stack: {ex.StackTrace}");
            }
        }
    }
}