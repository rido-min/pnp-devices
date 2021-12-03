using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Client.Connecting;
using MQTTnet.Client.Options;
using MQTTnet.Client.Publishing;
using MQTTnet.Client.Subscribing;
using Rido.IoTHubClient;
using System.Diagnostics;

namespace pnp_memmon
{
    public class HiveConnection : IMqttConnection
    {
        public Func<MqttApplicationMessageReceivedEventArgs, Task> OnMessage { get; set; }

        public event EventHandler<DisconnectEventArgs> OnMqttClientDisconnected; // { get; set; }

        public ConnectionSettings ConnectionSettings { get; private set; }

        IMqttClient mqttClient;
        private bool disposedValue;

        public static async Task<IMqttConnection> CreateAsync(ConnectionSettings connectionSettings, CancellationToken cancellationToken)
        {
            var conn = new HiveConnection(connectionSettings);
            await conn.ConnectAsync(cancellationToken);
            return conn;
        }

        private HiveConnection(ConnectionSettings connectionSettings)
        {
            ConnectionSettings = connectionSettings;
            mqttClient = new MqttFactory().CreateMqttClient();
            mqttClient.UseApplicationMessageReceivedHandler(m => OnMessage?.Invoke(m));
            mqttClient.UseDisconnectedHandler(e =>
            {
                OnMqttClientDisconnected?.Invoke(this, new DisconnectEventArgs()
                {
                    Exception = e.Exception,
                    DisconnectReason = (DisconnectReason)e.Reason,
                });
            });
        }

        private async Task ConnectAsync(CancellationToken cancellationToken)
        {
            var connAck = await mqttClient.ConnectAsync(new MqttClientOptionsBuilder()
                                 .WithTcpServer(ConnectionSettings.HostName, 8883).WithTls()
                                 .WithClientId(ConnectionSettings.DeviceId)
                                 .WithCredentials(ConnectionSettings.DeviceId, ConnectionSettings?.SharedAccessKey)
                                 .Build(),
                                 cancellationToken);

            if (connAck?.ResultCode != MqttClientConnectResultCode.Success)
            {
                throw new ApplicationException($"Error connecting: {connAck?.ResultCode} {connAck?.ReasonString}");
            }
        }

        public bool IsConnected => mqttClient.IsConnected;

        public async Task CloseAsync() => await mqttClient.DisconnectAsync();

        public Task<MqttClientPublishResult> PublishAsync(string topic, object payload) => PublishAsync(topic, payload, CancellationToken.None);
        public async Task<MqttClientPublishResult> PublishAsync(string topic, object payload, CancellationToken cancellationToken)
        {
            string jsonPayload;
            if (payload is string)
            {
                jsonPayload = payload as string;
            }
            else
            {
                jsonPayload = System.Text.Json.JsonSerializer.Serialize(payload);
            }
            var message = new MqttApplicationMessageBuilder()
                              .WithTopic(topic)
                              .WithPayload(jsonPayload)
                              .Build();

            if (mqttClient != null)
            {
                var pubRes = await mqttClient.PublishAsync(message, cancellationToken);
                if (pubRes.ReasonCode != MqttClientPublishReasonCode.Success)
                {
                    Trace.TraceError(pubRes.ReasonCode + pubRes.ReasonString);
                }
                return pubRes;
            }
            return new MqttClientPublishResult() { ReasonCode = MqttClientPublishReasonCode.UnspecifiedError };
        }

        public Task<MqttClientSubscribeResult> SubscribeAsync(string topic, CancellationToken cancellationToken = default) => SubscribeAsync(new string[] { topic });
        public async Task<MqttClientSubscribeResult> SubscribeAsync(string[] topics, CancellationToken cancellationToken = default)
        {
            //subscribedTopics = topics;
            var subBuilder = new MqttClientSubscribeOptionsBuilder();
            topics.ToList().ForEach(t => subBuilder.WithTopicFilter(t, MQTTnet.Protocol.MqttQualityOfServiceLevel.AtLeastOnce));
            return await mqttClient.SubscribeAsync(subBuilder.Build());
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    mqttClient.Dispose();
                }
                disposedValue = true;
            }
        }
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
