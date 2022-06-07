using GatewayServer.Services;
using Microsoft.Azure.Devices.Client;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace GatewayServer.Utils
{
    public class ServiceHealthCheck : IHealthCheck
    {
        private RunnerConfiguration config;

        public ServiceHealthCheck(RunnerConfiguration config)
        {
            this.config = config;
        }

        public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {

            if(!string.IsNullOrEmpty(config.TestDeviceId))
            {
                try
                {
                    
                    var device = DeviceClient.CreateFromConnectionString(
                        config.IotHubConnectionString,
                        config.TestDeviceId,
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
                    device.OpenAsync().Wait();
                    device.CloseAsync().Wait();
                    return Task.FromResult(HealthCheckResult.Healthy($"Test device ({config.TestDeviceId}) connectivity established"));
                }
                catch(Exception ex)
                {
                    return Task.FromResult(
                        new HealthCheckResult(
                            context.Registration.FailureStatus, $"Test device ({config.TestDeviceId}) connectivity failed with ({ex.Message})."));
                }
            }
            else
            {
                return Task.FromResult(HealthCheckResult.Healthy("Skipped testing device connectivity"));
            }
        }

        public static Task WriteResponse(HttpContext context, HealthReport healthReport)
        {
            context.Response.ContentType = "application/json; charset=utf-8";

            var options = new JsonWriterOptions { Indented = true };

            using var memoryStream = new MemoryStream();
            using (var jsonWriter = new Utf8JsonWriter(memoryStream, options))
            {
                jsonWriter.WriteStartObject();
                jsonWriter.WriteString("status", healthReport.Status.ToString());
                jsonWriter.WriteStartObject("results");

                foreach (var healthReportEntry in healthReport.Entries)
                {
                    jsonWriter.WriteStartObject(healthReportEntry.Key);
                    jsonWriter.WriteString("status",
                        healthReportEntry.Value.Status.ToString());
                    jsonWriter.WriteString("description",
                        healthReportEntry.Value.Description);
                    jsonWriter.WriteStartObject("data");

                    foreach (var item in healthReportEntry.Value.Data)
                    {
                        jsonWriter.WritePropertyName(item.Key);

                        JsonSerializer.Serialize(jsonWriter, item.Value,
                            item.Value?.GetType() ?? typeof(object));
                    }

                    jsonWriter.WriteEndObject();
                    jsonWriter.WriteEndObject();
                }

                jsonWriter.WriteEndObject();
                jsonWriter.WriteEndObject();
            }

            return context.Response.WriteAsync(
                Encoding.UTF8.GetString(memoryStream.ToArray()));
        }
    }
}
