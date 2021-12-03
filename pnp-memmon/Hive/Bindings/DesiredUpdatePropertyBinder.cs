using System;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

namespace Rido.IoTHubClient.HiveTopicBinders
{
    public class DesiredUpdatePropertyBinder<T>
    {
        public Func<WritableProperty<T>, Task<WritableProperty<T>>> OnProperty_Updated = null;
        public DesiredUpdatePropertyBinder(IMqttConnection connection, string propertyName, string componentName = "")
        {
            _ = connection.SubscribeAsync("pnp/+/props/set/#");
            //UpdateTwinBinder updateTwin = new UpdateTwinBinder(connection);
            UpdatePropertyBinder propertyBinder = new UpdatePropertyBinder(connection);
            connection.OnMessage += async m =>
            {
                var topic = m.ApplicationMessage.Topic;
                if (topic.StartsWith($"pnp/{connection.ConnectionSettings.DeviceId}/props/set"))
                {
                    string msg = Encoding.UTF8.GetString(m.ApplicationMessage.Payload ?? Array.Empty<byte>());
                    JsonNode desired = JsonNode.Parse(msg);
                    var desiredProperty = desired?[propertyName];
                    if (desiredProperty != null)
                    {
                        if (OnProperty_Updated != null)
                        {
                            var property = new WritableProperty<T>(propertyName, componentName)
                            {
                                Value = desiredProperty.GetValue<T>(),
                                Version = desired?["$version"]?.GetValue<int>() ?? 0
                            };
                            var ack = await OnProperty_Updated(property);
                            if (ack != null)
                            {
                                //_ = updateTwin.SendRequestWaitForResponse(ack);
                                _ = propertyBinder.SendRequestWaitForResponse(ack.ToAck());
                            }
                        }
                    }
                }
            };
        }
    }
}
