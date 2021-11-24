﻿//  <auto-generated/> 
#nullable enable

using MQTTnet.Client.Publishing;
using Rido.IoTHubClient;
using System.Diagnostics;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Web;

namespace dtmi_rido_pnp
{
    public class memmon
    {
        const string modelId = "dtmi:rido:pnp:memmon;1";
        internal IMqttConnection _connection;
        string initialTwin = string.Empty;
        int lastRid;

        public ConnectionSettings ConnectionSettings => _connection.ConnectionSettings;
        public Func<WritableProperty<bool>, Task<WritableProperty<bool>>>? OnProperty_enabled_Updated = null;
        public Func<WritableProperty<int>, Task<WritableProperty<int>>>? OnProperty_interval_Updated = null;
        public Func<Cmd_getRuntimeStats_Request, Task<Cmd_getRuntimeStats_Response>>? OnCommand_getRuntimeStats_Invoked = null;

        public WritableProperty<bool>? Property_enabled;
        public WritableProperty<int>? Property_interval;
        public DateTime Property_started { get; private set; }

        private memmon(IMqttConnection c)
        {
            _connection = c;
            ConfigureSysTopicsCallbacks(_connection);
        }

        public static async Task<memmon> CreateDeviceClientAsync(string cs, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(cs);
            var connection = await HubMqttConnection.CreateAsync(new ConnectionSettings(cs) { ModelId = modelId }, cancellationToken);
            await SubscribeToSysTopicsAsync(connection);
            var client = new memmon(connection);
            client.initialTwin = await client.GetTwinAsync();
            return client;
        }

        public async Task InitProperty_enabled_Async(bool defaultEnabled)
        {
            Property_enabled = WritableProperty<bool>.InitFromTwin(initialTwin, "enabled", defaultEnabled);
            if (OnProperty_enabled_Updated != null && (Property_enabled.DesiredVersion > 1))
            {
                var ack = await OnProperty_enabled_Updated.Invoke(Property_enabled);
                await UpdateTwin(ack.ToAck());
                Property_enabled = ack;
            }
            else
            {
                await UpdateTwin(Property_enabled.ToAck());
            }
        }

        public async Task InitProperty_interval_Async(int defaultInterval)
        {
            Property_interval = WritableProperty<int>.InitFromTwin(initialTwin, "interval", defaultInterval);
            if (OnProperty_interval_Updated != null && (Property_interval.DesiredVersion > 1))
            {
                var ack = await OnProperty_interval_Updated.Invoke(Property_interval);
                await UpdateTwin(ack.ToAck());
                Property_interval = ack;
            }
            else
            {
                await UpdateTwin(Property_interval.ToAck());
            }
        }

        void ConfigureSysTopicsCallbacks(IMqttConnection connection)
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

                if (topic.StartsWith("$iothub/methods/POST/getRuntimeStats"))
                {
                    Cmd_getRuntimeStats_Request req = new Cmd_getRuntimeStats_Request()
                    {
                        DiagnosticsMode = JsonSerializer.Deserialize<DiagnosticsMode>(msg),
                        _rid = rid
                    };
                    if (OnCommand_getRuntimeStats_Invoked != null)
                    {
                        var resp = await OnCommand_getRuntimeStats_Invoked.Invoke(req);
                        await _connection.PublishAsync($"$iothub/methods/res/{resp?.Status}/?$rid={rid}", resp);
                    }
                }

                if (topic.StartsWith("$iothub/twin/res/200"))
                {
                    this.getTwin_cb?.Invoke(msg);
                }

                if (topic.StartsWith("$iothub/twin/res/204"))
                {
                    this.report_cb?.Invoke(twinVersion);
                }

                if (topic.StartsWith("$iothub/twin/PATCH/properties/desired"))
                {
                    JsonNode? root = JsonNode.Parse(msg);
                    await Invoke_enabled_Callback(root);
                    await Invoke_interval_Callback(root);
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
                        await _connection.PublishAsync($"$iothub/twin/PATCH/properties/reported/?$rid={lastRid++}", ack.ToAck());
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
                        await _connection.PublishAsync($"$iothub/twin/PATCH/properties/reported/?$rid={lastRid++}", ack.ToAck());
                    }
                }
            }
        }

        private static async Task SubscribeToSysTopicsAsync(IMqttConnection connection)
        {
            var subres = await connection.SubscribeAsync(new string[] {
                                                    "$iothub/methods/POST/#",
                                                    "$iothub/twin/res/#",
                                                    "$iothub/twin/PATCH/properties/desired/#"});

            subres.Items.ToList().ForEach(x => Trace.TraceInformation($"+ {x.TopicFilter.Topic} {x.ResultCode}"));
        }

        Action<string>? getTwin_cb;
        public async Task<string> GetTwinAsync()
        {
            var tcs = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
            var puback = await _connection.PublishAsync($"$iothub/twin/GET/?$rid={lastRid++}", string.Empty);
            if (puback?.ReasonCode == MqttClientPublishReasonCode.Success)
            {
                getTwin_cb = s => tcs.TrySetResult(s);
            }
            else
            {
                getTwin_cb = s => tcs.TrySetException(new ApplicationException($"Error '{puback?.ReasonCode}' publishing twin GET: {s}"));
            }
            return await tcs.Task.TimeoutAfter(TimeSpan.FromSeconds(5));
        }

        Action<int>? report_cb;

        public async Task<int> Report_started_Async(DateTime started)
        {
            var tcs = new TaskCompletionSource<int>();
            var puback = await _connection.PublishAsync($"$iothub/twin/PATCH/properties/reported/?$rid={lastRid++}", new { started });
            if (puback.ReasonCode == MqttClientPublishReasonCode.Success)
            {
                report_cb = s => tcs.TrySetResult(s);
                Property_started = started;
            }
            else
            {
                report_cb = s => tcs.TrySetException(new ApplicationException($"Error '{puback.ReasonCode}' publishing twin PATCH: {s}"));
            }
            return await tcs.Task;
        }

        public async Task<int> UpdateTwin(object patch)
        {
            var tcs = new TaskCompletionSource<int>();
            var puback = await _connection.PublishAsync(
                    $"$iothub/twin/PATCH/properties/reported/?$rid={lastRid++}",
                        patch);
            if (puback.ReasonCode == MqttClientPublishReasonCode.Success)
            {
                report_cb = s => tcs.TrySetResult(s);
            }
            else
            {
                report_cb = s => tcs.TrySetException(new ApplicationException($"Error '{puback.ReasonCode}' publishing twin PATCH: {s}"));
            }
            return await tcs.Task.TimeoutAfter(TimeSpan.FromSeconds(15));
        }

        public async Task<MqttClientPublishResult> Send_workingSet_Async(double workingSet) => await Send_workingSet_Async(workingSet, CancellationToken.None);
        public async Task<MqttClientPublishResult> Send_workingSet_Async(double workingSet, CancellationToken cancellationToken)
        {
            return await _connection.PublishAsync(
                $"devices/{_connection.ConnectionSettings.DeviceId}/messages/events/",
                new { workingSet }, cancellationToken);
        }
    }
}
