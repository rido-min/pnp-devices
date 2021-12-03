using MQTTnet.Client.Publishing;
using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Rido.IoTHubClient.HiveTopicBinders
{
    public class UpdatePropertyBinder
    {
        readonly IMqttConnection connection;

        public UpdatePropertyBinder(IMqttConnection connection)
        {
            this.connection = connection;
        }

        public async Task<MqttClientPublishResult> SendRequestWaitForResponse(object payload, int timeout = 5)
        {
            var rid = RidCounter.NextValue();
            
            return await connection.PublishAsync($"pnp/{connection.ConnectionSettings.DeviceId}/props/reported/?$rid={rid}", payload);
            
        }
    }
}
