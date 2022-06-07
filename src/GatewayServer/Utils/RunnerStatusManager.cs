using GatewayServer.Models;
using Microsoft.Azure.Devices.Client;
using Microsoft.Extensions.Caching.Memory;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GatewayServer.Utils
{
    public static class RunnerStatusManager
    {
        const long ReportRate = 10000;
        
        public static RunnerStatus RunnerSavedState { get; }

        public static long MessagesSent => RunnerSavedState.messagesSent;

        public static long TotalSendTelemetryErrors => RunnerSavedState.totalSendTelemetryErrors;

        public static long ConnectedDevices => RunnerSavedState.connectedDevices;

        static RunnerStatusManager()
        {
            RunnerSavedState = new RunnerStatus();
            RunnerSavedState.messagesSendingStart = DateTime.UtcNow.Ticks;
        }

        internal static void IncrementDeviceConnected()
        {
            var newValue = Interlocked.Increment(ref RunnerSavedState.connectedDevices);
            if (newValue % ReportRate == 0)
                Console.WriteLine($"{DateTime.UtcNow.ToString("o")}: {newValue} devices connected");
        }

        internal static void DecrementDeviceConnected()
        {
            var newValue = Interlocked.Decrement(ref RunnerSavedState.connectedDevices);
            if (newValue % ReportRate == 0)
                Console.WriteLine($"{DateTime.UtcNow.ToString("o")}: (D) {newValue} devices connected");
        }

        internal static void IncrementCompletedDevice()
        {
            var newValue = Interlocked.Increment(ref RunnerSavedState.completedDevices);
            if (newValue % ReportRate == 0)
                Console.WriteLine($"{DateTime.UtcNow.ToString("o")}: {newValue} devices have completed sending messages");
        }

        internal static void IncrementMessageSent()
        {
            var newValue = Interlocked.Increment(ref RunnerSavedState.messagesSent);
            if (newValue % ReportRate == 0)
            {
                var now = DateTime.UtcNow;
                var currentStart = Interlocked.Exchange(ref RunnerSavedState.messagesSendingStart, now.Ticks);
                var start = new DateTime(currentStart, DateTimeKind.Utc);
                var elapsedMs = (now - start).TotalMilliseconds;
                var ratePerSecond = ReportRate / elapsedMs * 1000;

                Console.WriteLine($"{DateTime.UtcNow.ToString("o")}: {newValue} total messages have been sent @ {ratePerSecond.ToString("0.00")} msgs/sec");
            }
        }

        internal static void IncrementSendTelemetryErrors()
        {
            var newValue = Interlocked.Increment(ref RunnerSavedState.totalSendTelemetryErrors);
            if (newValue % ReportRate == 0)
                Console.WriteLine($"{DateTime.UtcNow.ToString("o")}: {newValue} errors sending telemetry");
        }

        internal static void IncrementSendTelemetryTransientErrors()
        {
            var newValue = Interlocked.Increment(ref RunnerSavedState.totalSendTelemetryTransientErrors);
            if (newValue % ReportRate == 0)
                Console.WriteLine($"{DateTime.UtcNow.ToString("o")}: {newValue} trans. errors sending telemetry");
        }

        internal static void OnPostEviction(object key, object deviceCache, EvictionReason reason, object state)
        {
            if (deviceCache is DeviceClient device)
            {
                try
                {
                    device.CloseAsync();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Gateway CACHE: evection failed to close ({key}) connection. Reason: {ex.Message}");
                }

                Console.WriteLine($"Gateway CACHE: evection success of ({key}) for ({reason})");

                DecrementDeviceConnected();
            }
        }
    }
}
