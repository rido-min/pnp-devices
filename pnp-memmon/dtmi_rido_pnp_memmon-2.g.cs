﻿//  <auto-generated/> 
#nullable enable

using MQTTnet.Client.Publishing;
using pnp_memmon;
using Rido.IoTHubClient;
using System.Diagnostics;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Web;

namespace dtmi_rido_pnp
{



    public class memmon_mqtt
    {
        const string modelId = "dtmi:rido:pnp:memmon;1";
        internal IHubMqttConnection _connection;

        int lastRid;

        public Func<WritableProperty<bool>, Task<WritableProperty<bool>>>? OnProperty_enabled_Updated = null;
        public Func<WritableProperty<int>, Task<WritableProperty<int>>>? OnProperty_interval_Updated = null;
        public Func<Cmd_getRuntimeStats_Request, Task<Cmd_getRuntimeStats_Response>>? OnCommand_getRuntimeStats_Invoked = null;

        public WritableProperty<bool>? Property_enabled { get; set; }
        public WritableProperty<int>? Property_interval { get; set; }
        public DateTime Property_started { get; private set; }

        public ConnectionSettings? ConnectionSettings => _connection.ConnectionSettings;

        public memmon_mqtt(IHubMqttConnection c)
        {
            _connection = c;
            ConfigureSysTopicsCallbacks(_connection);
        }

        public static async Task<memmon_mqtt> CreateDeviceClientAsync(string cs)
        {
            ArgumentNullException.ThrowIfNull(cs);
            var connectionSettings = new ConnectionSettings(cs);
            IHubMqttConnection conn = await HiveConnection.CreateAsync(connectionSettings);
            await SubscribeToSysTopicsAsync(conn);
            return new memmon_mqtt(conn);
        }

        public async Task InitProperty_enabled_Async(bool defaultEnabled)
        {
            //var twin = await GetTwinAsync();
            Property_enabled = new WritableProperty<bool>("enabled") { Value = defaultEnabled };
            //OnProperty_enabled_Updated?.Invoke(Property_enabled);
            await UpdateTwin(Property_enabled.ToAck());
        }

        public async Task InitProperty_interval_Async(int defaultInterval)
        {
            Property_interval = new WritableProperty<int>("interval") { Value = defaultInterval };
            await UpdateTwin(Property_interval.ToAck());
        }

        void ConfigureSysTopicsCallbacks(IHubMqttConnection connection)
        {
            connection.OnMessage = async m =>
            {
                var topic = m.ApplicationMessage.Topic;
                var segments = topic.Split('/');
                int rid = 0;
                int twinVersion = 0;
                if (topic.Contains('?'))
                {
                    // parse qs to extract the rid
                    var qs = HttpUtility.ParseQueryString(segments[^1]);
                    rid = Convert.ToInt32(qs["$rid"]);
                    twinVersion = Convert.ToInt32(qs["$version"]);
                }

                string msg = Encoding.UTF8.GetString(m.ApplicationMessage.Payload ?? Array.Empty<byte>());

                if (topic.StartsWith($"pnp/{ConnectionSettings?.DeviceId}/props/set"))
                {
                    JsonNode? root = JsonNode.Parse(msg);
                    await Invoke_interval_Callback(root);
                    await Invoke_enabled_Callback(root);
                }

                if (topic.StartsWith($"pnp/{ConnectionSettings?.DeviceId}/commands/getRuntimeStats"))
                {
                    Cmd_getRuntimeStats_Request req = new Cmd_getRuntimeStats_Request()
                    {
                        DiagnosticsMode = JsonSerializer.Deserialize<DiagnosticsMode>(msg),
                        //_rid = rid
                    };
                    if (OnCommand_getRuntimeStats_Invoked != null)
                    {
                        var resp = await OnCommand_getRuntimeStats_Invoked.Invoke(req);
                        await _connection.PublishAsync($"pnp/{ConnectionSettings?.DeviceId}/commands/getRuntimeStats/resp/{resp?.Status}/?$rid={rid}", resp);
                    }
                }
            };
        }

        private async Task Invoke_interval_Callback(JsonNode? desired)
        {
            if (desired?["interval"] != null)
            {
                if (OnProperty_interval_Updated != null)
                {
                    var intervalProperty = new WritableProperty<int>("interval")
                    {
                        Value = Convert.ToInt32(desired?["interval"]?.GetValue<int>()),
                        Version = desired?["$version"]?.GetValue<int>() ?? 0
                    };
                    var ack = await OnProperty_interval_Updated.Invoke(intervalProperty);
                    if (ack != null)
                    {
                        Property_interval = ack;
                        await _connection.PublishAsync($"pnp/{ConnectionSettings?.DeviceId}/props/reported/?$rid={lastRid++}", ack.ToAck());
                    }
                }
            }
        }

        private async Task Invoke_enabled_Callback(JsonNode? desired)
        {
            if (desired?["enabled"] != null)
            {
                if (OnProperty_enabled_Updated != null)
                {
                    var enabledProperty = new WritableProperty<bool>("enabled")
                    {
                        Value = Convert.ToBoolean(desired?["enabled"]?.GetValue<bool>()),
                        Version = desired?["$version"]?.GetValue<int>() ?? 0,
                    };
                    var ack = await OnProperty_enabled_Updated.Invoke(enabledProperty);
                    if (ack != null)
                    {
                        Property_enabled = ack;
                        await _connection.PublishAsync($"pnp/{ConnectionSettings?.DeviceId}/props/reported/?$rid={lastRid++}", ack.ToAck());
                    }
                }
            }
        }

        private static async Task SubscribeToSysTopicsAsync(IHubMqttConnection connection)
        {
            var subres = await connection.SubscribeAsync(new string[]
                                                            {
                                                                "pnp/+/commands/#",
                                                                "pnp/+/props/set/#"
                                                            });
            subres.Items.ToList().ForEach(x => Trace.TraceInformation($"+ {x.TopicFilter.Topic} {x.ResultCode}"));
        }

        public async Task<MqttClientPublishResult> Report_started_Async(DateTime started) =>
            await _connection.PublishAsync($"pnp/{ConnectionSettings?.DeviceId}/props/reported/?$rid={lastRid++}", new { started });

        public async Task<MqttClientPublishResult> UpdateTwin(object payload) =>
            await _connection.PublishAsync($"pnp/{ConnectionSettings?.DeviceId}/props/reported/?$rid={lastRid++}", payload);

        public async Task<MqttClientPublishResult> Send_workingSet_Async(double workingSet) =>
            await _connection.PublishAsync($"pnp/{ConnectionSettings?.DeviceId}/telemetry", new { workingSet });


    }
}
