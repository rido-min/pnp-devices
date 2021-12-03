﻿//  <auto-generated/> 
#nullable enable

using pnp_memmon;
using Rido.IoTHubClient;
using Rido.IoTHubClient.HiveTopicBinders;

namespace dtmi_rido_pnp
{
    public class memmon_hive //: BasicHubClient
    {
        const string modelId = "dtmi:rido:pnp:memmon;1";
        public IMqttConnection Connection;
        public DateTime Property_started { get; private set; }

        public UpdatePropertyBinder updatePropertyBinder;

        public Bound_Property<bool> Property_enabled;
        public Bound_Property<int> Property_interval;
        public CommandBinder<Cmd_getRuntimeStats_Request, Cmd_getRuntimeStats_Response> Command_getRuntimeStats_Binder;

        public TelemetryBinder telemetryBinder;

        public string InitialTwin = "{}";

        public static async Task<memmon_hive> CreateDeviceClientAsync(string cs, CancellationToken cancellationToken = default(CancellationToken))
        {
            ArgumentNullException.ThrowIfNull(cs);
            var connection = await HiveConnection.CreateAsync(new ConnectionSettings(cs) { ModelId = modelId }, cancellationToken);
            var client = new memmon_hive(connection);
            //client.InitialTwin = await client.GetTwinAsync();
            return client;
        }

        private memmon_hive(IMqttConnection c) //: base(c)
        {
            Connection = c;
            updatePropertyBinder = new UpdatePropertyBinder(Connection);    
            telemetryBinder = new TelemetryBinder(Connection);

            Property_interval = new Bound_Property<int>(Connection, "interval");
            Property_enabled = new Bound_Property<bool>(Connection, "enabled"); ;
            Command_getRuntimeStats_Binder = new CommandBinder<Cmd_getRuntimeStats_Request, Cmd_getRuntimeStats_Response>(Connection, "getRuntimeStats");
        }

        public async Task<int> Report_started_Async(DateTime started)
        {
            var res = await updatePropertyBinder.SendRequestWaitForResponse(new { started });
            return 1;
        }

        public async Task<PubResult> Send_workingSet_Async(double workingSet, CancellationToken cancellationToken = default(CancellationToken)) => 
            await telemetryBinder.SendTelemetryAsync(new { workingSet });
            
    }
}
