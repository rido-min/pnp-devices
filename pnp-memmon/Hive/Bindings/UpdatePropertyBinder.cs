using MQTTnet.Client.Publishing;
using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Rido.IoTHubClient.HiveTopicBinders
{
    public class UpdatePropertyBinder
    {
        readonly ConcurrentDictionary<int, TaskCompletionSource<int>> pendingRequests;
        readonly IMqttConnection connection;

        public UpdatePropertyBinder(IMqttConnection connection)
        {
            pendingRequests = new ConcurrentDictionary<int, TaskCompletionSource<int>>();
            this.connection = connection;
            _ = connection.SubscribeAsync("pnp/+/props/reported/#");
        }

        public async Task<MqttClientPublishResult> SendRequestWaitForResponse(object payload, int timeout = 5)
        {
            var rid = RidCounter.NextValue();
            
            return await connection.PublishAsync($"pnp/{connection.ConnectionSettings.DeviceId}/props/reported/?$rid={rid}", payload);
            
        }
    }
}
