using GatewayServer.Utils;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GatewayServer.Models
{
    public abstract class SenderBase<TMessage> : ISender
    {
        protected const int WaitTimeOnTransientError = 5_000;
        protected const int MaxSendAttempts = 3;

        private readonly string deviceId;

        protected RunnerConfiguration Config { get; }

        public SenderBase(string deviceId, RunnerConfiguration config)
        {
            this.deviceId = deviceId;
            this.Config = config;
        }

        public abstract Task OpenAsync();

        public async Task SendMessageAsync(string message, RunnerStats stats, CancellationToken cancellationToken)
        {
            var msg = this.CreateMessage(message);

            for (var attempt = 1; attempt <= MaxSendAttempts; ++attempt)
            {
                try
                {
                    await this.SendAsync(msg, cancellationToken);
                    stats.IncrementMessageSent();
                    break;
                    //attempt = 1;
                }
                catch (Exception ex) when (this.IsTransientException(ex))
                {
                    stats.IncrementSendTelemetryErrors();
                    await Task.Yield();
                }
                catch (Exception)
                {
                    stats.IncrementSendTelemetryErrors();
                    await Task.Delay(WaitTimeOnTransientError);
                }
            }
        }

        protected abstract Task SendAsync(TMessage msg, CancellationToken cancellationToken);

        protected TMessage CreateMessage(string message)
        {
            TMessage msg = this.BuildMessage(message);

            return msg;
        }

        protected abstract TMessage BuildMessage(string messageString);

        protected abstract bool IsTransientException(Exception exception);
    }
}
