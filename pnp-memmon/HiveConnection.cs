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
        public Func<MqttApplicationMessageReceivedEventArgs, Task>? OnMessage { get; set; }

        public event EventHandler<DisconnectEventArgs>? OnMqttClientDisconnected; // { get; set; }

        public ConnectionSettings? ConnectionSettings { get; private set; }

        IMqttClient mqttClient;
        private bool disposedValue;

        public HiveConnection(ConnectionSettings connectionSettings)
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

        public async Task ConnectAsync()
        {
            var connAck = await mqttClient.ConnectAsync(new MqttClientOptionsBuilder()
                                 .WithTcpServer(ConnectionSettings?.HostName, 8883).WithTls()
                                 .WithClientId(ConnectionSettings?.DeviceId)
                                 .WithCredentials(ConnectionSettings?.DeviceId, ConnectionSettings?.SharedAccessKey)
                                 .Build(),
                                 CancellationToken.None);
            if (connAck?.ResultCode != MqttClientConnectResultCode.Success)
            {
                throw new ApplicationException($"Error connecting: {connAck?.ResultCode} {connAck?.ReasonString}");
            }
        }

        
        public static async Task<IMqttConnection> CreateAsync(ConnectionSettings connectionSettings)
        {
            var conn = new HiveConnection(connectionSettings);
            await conn.ConnectAsync();
            return conn;
        }


        public bool IsConnected => mqttClient.IsConnected;

        public async Task CloseAsync() => await mqttClient.DisconnectAsync();


        public async Task<MqttClientPublishResult> PublishAsync(string topic, object payload)
        {

            string? jsonPayload;
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
                var pubRes = await mqttClient.PublishAsync(message, CancellationToken.None);
                if (pubRes.ReasonCode != MqttClientPublishReasonCode.Success)
                {
                    Trace.TraceError(pubRes.ReasonCode + pubRes.ReasonString);
                }
                return pubRes;
            }
            return new MqttClientPublishResult() { ReasonCode = MqttClientPublishReasonCode.UnspecifiedError };
        }

        public async Task<MqttClientSubscribeResult> SubscribeAsync(string[] topics)
        {
            //subscribedTopics = topics;
            var subBuilder = new MqttClientSubscribeOptionsBuilder();
            topics.ToList().ForEach(t => subBuilder.WithTopicFilter(t, MQTTnet.Protocol.MqttQualityOfServiceLevel.AtLeastOnce));
            return await mqttClient.SubscribeAsync(subBuilder.Build());
        }

        public Task<MqttClientPublishResult> PublishAsync(string topic, object payload, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects)
                }

                // TODO: free unmanaged resources (unmanaged objects) and override finalizer
                // TODO: set large fields to null
                disposedValue = true;
            }
        }

        // // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
        // ~HiveConnection()
        // {
        //     // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        //     Dispose(disposing: false);
        // }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
