using MQTTnet.Client.Publishing;
using Rido.IoTHubClient;
using System.Diagnostics;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Web;

namespace dtmi_rido
{
    public class wprops_demo
    {
        const string modelId = "dtmi:rido:wprops_demo;1";
        internal IHubMqttConnection _connection;

        int lastRid;

        public Func<WritableProperty<bool>, Task<WritableProperty<bool>>>? OnProperty_enabled_Updated = null;
        public Func<WritableProperty<int>, Task<WritableProperty<int>>>? OnProperty_interval_Updated = null;
        public Func<Cmd_getRuntimeStats_Request, Task<Cmd_getRuntimeStats_Response>>? OnCommand_getRuntimeStats_Invoked = null;

        public WritableProperty<bool>? Property_enabled;
        public WritableProperty<int>? Property_interval;

        public wprops_demo(IHubMqttConnection c)
        {
            _connection = c;
            ConfigureSysTopicsCallbacks(_connection);
        }

        public static async Task<wprops_demo> CreateDeviceClientAsync(string cs)
        {
            var connection = await HubMqttConnection.CreateAsync(new ConnectionSettings(cs) { ModelId = modelId });
            await SubscribeToSysTopicsAsync(connection);
            var client = new wprops_demo(connection);
            return client;
        }

        public async Task InitTwinAsync(bool defaultEnabled, int defaultInterval)
        {
            var twin = await GetTwinAsync();
            var root = JsonNode.Parse(twin);
            var desired = root?["desired"];
            var reported = root?["reported"];
            var desiredVersion = desired?["$version"]?.GetValue<int>();
            var reported_enabled_version = reported?["enabled"]?["av"]?.GetValue<int>();
            var reported_interval_version = reported?["interval"]?["av"]?.GetValue<int>();

            if (reported_enabled_version == null)
            {
                Property_enabled = new WritableProperty<bool>("enabled")
                {
                    Version = desiredVersion,
                    Status = 203,
                    Description = "init with default values from device",
                    Value = defaultEnabled
                };
                await _connection.PublishAsync($"$iothub/twin/PATCH/properties/reported/?$rid={lastRid++}", Property_enabled.ToAck());
            }
            else
            {
                Property_enabled = new WritableProperty<bool>("enabled")
                {
                    Version = reported_interval_version,
                    Status = reported?["enabled"]?["ac"]?.GetValue<int>(),
                    Description = reported?["enabled"]?["ad"]?.GetValue<string>(),
                    Value = reported?["enabled"]?["value"]?.GetValue<bool>() ?? true
                };
            }

            if (desiredVersion > reported_enabled_version)
            {
                if (OnProperty_enabled_Updated != null)
                {
                    var desired_enabled = desired?["enabled"]?.GetValue<bool>();
                    Property_enabled = await OnProperty_enabled_Updated.Invoke(
                        new WritableProperty<bool>("enabled")
                        {
                            Description = "updated on init",
                            Status = 201,
                            Version = desiredVersion,
                            Value = Convert.ToBoolean(desired_enabled)
                        });
                }
            }

            if (reported_interval_version == null)
            {
                Property_interval = new WritableProperty<int>("interval")
                {
                    Version = desiredVersion,
                    Status = 203,
                    Description = "init with default device value",
                    Value = defaultInterval
                };
                await _connection.PublishAsync($"$iothub/twin/PATCH/properties/reported/?$rid={lastRid++}", Property_interval.ToAck());
            }
            else
            {
                Property_interval = new WritableProperty<int>("interval")
                {
                    Version = reported_interval_version,
                    Status = reported?["interval"]?["ac"]?.GetValue<int>(),
                    Description = reported?["interval"]?["ad"]?.GetValue<string>(),
                    Value = reported?["interval"]?["value"]?.GetValue<int>() ?? 0
                };
            }

            if (desiredVersion > reported_interval_version)
            {
                if (OnProperty_interval_Updated != null)
                {
                    var desired_interval = desired?["interval"]?.GetValue<int>();
                    Property_interval = await OnProperty_interval_Updated.Invoke(
                         new WritableProperty<int>("interval")
                         {
                             Description = "updated on init",
                             Status = 201,
                             Version = desiredVersion,
                             Value = Convert.ToInt32(desired_interval)
                         });
                }
            } 
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
                        await _connection.PublishAsync($"$iothub/methods/res/{resp?._status}/?$rid={resp?._rid}", resp);
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
                        Version = desired?["$version"]?.GetValue<int>()
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
                        Version = desired?["$version"]?.GetValue<int>(),
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

        private static async Task SubscribeToSysTopicsAsync(HubMqttConnection connection)
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
            }
            else
            {
                report_cb = s => tcs.TrySetException(new ApplicationException($"Error '{puback.ReasonCode}' publishing twin PATCH: {s}"));
            }
            return tcs.Task.Result;
        }

        public async Task<int> Report_enabled_Async(WritableProperty<bool> enabledProp)
        {
            var tcs = new TaskCompletionSource<int>();
            var puback = await _connection.PublishAsync($"$iothub/twin/PATCH/properties/reported/?$rid={lastRid++}", enabledProp.ToAck());
            if (puback.ReasonCode == MqttClientPublishReasonCode.Success)
            {
                report_cb = s => tcs.TrySetResult(s);
            }
            else
            {
                report_cb = s => tcs.TrySetException(new ApplicationException($"Error '{puback.ReasonCode}' publishing twin PATCH: {s}"));
            }
            return tcs.Task.Result;
        }

        public async Task<int> Report_interval_Async(WritableProperty<int> intervalProp)
        {
            var tcs = new TaskCompletionSource<int>();
            var puback = await _connection.PublishAsync($"$iothub/twin/PATCH/properties/reported/?$rid={lastRid++}", intervalProp.ToAck());
            if (puback.ReasonCode == MqttClientPublishReasonCode.Success)
            {
                report_cb = s => tcs.TrySetResult(s);
            }
            else
            {
                report_cb = s => tcs.TrySetException(new ApplicationException($"Error '{puback.ReasonCode}' publishing twin PATCH: {s}"));
            }
            return tcs.Task.Result;
        }

        public async Task<MqttClientPublishResult> Send_workingSet_Async(double workingSet)
        {
            return await _connection.PublishAsync(
                $"devices/{_connection.ConnectionSettings.DeviceId}/messages/events/",
                new { workingSet });
        }
    }
}
