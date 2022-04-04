using GatewayServer.Models;
using GatewayServer.Utils;
using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.Client.Exceptions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GatewayServer.Services
{
    public class IoTHubSender : SenderBase<Message>
    {
        const string ApplicationJsonContentType = "application/json";
        const string Utf8Encoding = "utf-8";

        private readonly DeviceClient deviceClient;

        public IoTHubSender(DeviceClient deviceClient, string deviceId, RunnerConfiguration config)
            : base(deviceId, config)
        {
            this.deviceClient = deviceClient;
        }

        public override async Task OpenAsync()
        {
            await this.deviceClient.OpenAsync();
        }

        protected override async Task SendAsync(Message msg, CancellationToken cancellationToken)
        {
            await this.deviceClient.SendEventAsync(msg, cancellationToken);
        }

        protected override Message BuildMessage(string messageString)
        {
            var messageBytes = Encoding.UTF8.GetBytes(messageString);
            var msg = new Message(messageBytes)
            {
                CorrelationId = Guid.NewGuid().ToString(),
            };

            msg.ContentEncoding = Utf8Encoding;
            msg.ContentType = ApplicationJsonContentType;

            return msg;
        }

        protected override bool IsTransientException(Exception exception)
        {
            return exception is IotHubCommunicationException;
        }
    }
}
