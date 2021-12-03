using MQTTnet.Client.Publishing;
using Rido.IoTHubClient;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace pnp_memmon.HiveBindings
{
    public class DevicePropertyBinder
    {
        IMqttConnection connection;
        readonly ConcurrentDictionary<int, TaskCompletionSource<int>> pendingRequests;
        public DevicePropertyBinder(IMqttConnection connection)
        {
            this.connection = connection;
            pendingRequests = new ConcurrentDictionary<int, TaskCompletionSource<int>>();
        }

        public async Task<int> SendRequestWaitForResponse(object payload, int timeout = 5)
        {
            var rid = RidCounter.NextValue();
            var tcs = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
            var puback = await connection.PublishAsync($"pnp/{connection.ConnectionSettings.DeviceId}/props/reported/?$rid={rid}", payload);
            if (puback?.ReasonCode == MqttClientPublishReasonCode.Success)
            {
                pendingRequests.TryAdd(rid, tcs);
            }
            else
            {
                Trace.TraceError($"Error '{puback?.ReasonCode}' publishing twin GET");
            }
            return await tcs.Task.TimeoutAfter(TimeSpan.FromSeconds(timeout));
        }
    }
}
