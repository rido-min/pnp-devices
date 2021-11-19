﻿//  <auto-generated/> 
#nullable enable

using Microsoft.Azure.Devices;
using Microsoft.Azure.Devices.Shared;
using Rido.IoTHubServiceClient;
using System.Text.Json;

namespace dtmi_rido
{
    public class started : TwinProperty<DateTime> { }
    public class enabled : WritableProperty<bool> { }
    public class interval : WritableProperty<int> { }

    public class pnp_basic_service_api
    {
        static string js(object o) => System.Text.Json.JsonSerializer.Serialize(o);
        public started? started { get; set; }
        public enabled? enabled { get; set; }
        public interval? interval { get; set; }

        RegistryManager rm;
        ServiceClient sc;

        public pnp_basic_service_api(string cs)
        {
            rm = RegistryManager.CreateFromConnectionString(cs);
            sc = ServiceClient.CreateFromConnectionString(cs);
        }

        public async Task<Cmd_getRuntimeStats_Response> Invoke_getRuntimeStats_Async(string deviceId, Cmd_getRuntimeStats_Request req)
        {
            CloudToDeviceMethod method = new CloudToDeviceMethod("getRuntimeStats");
            method.SetPayloadJson(js(req.DiagnosticsMode));
            var resp = await sc.InvokeDeviceMethodAsync(deviceId, method);
            var result = JsonSerializer.Deserialize<Cmd_getRuntimeStats_Response>(resp.GetPayloadAsJson());
            return result ?? throw new ApplicationException("error deserializing command response");
        }

        public async Task<started> Read_started_Property(string deviceId)
        {
            var twin = await rm.GetTwinAsync(deviceId);
            started = new()
            {
                Value = Convert.ToDateTime(twin.Properties.Reported["started"].Value),
                Version = twin.Properties.Reported.Version
            };
            return started;
        }
        public async Task<interval> Read_interval_Property(string deviceId) 
        {
            var twin = await rm.GetTwinAsync(deviceId);
            int? desired_interval_value = twin.Properties.Desired["interval"];
            int? reported_interval_value = twin.Properties.Reported["interval"]?["value"];
            int? reported_interval_version = twin.Properties.Reported["interval"]?["av"];
            int? reported_interval_status = twin.Properties.Reported["interval"]?["ac"];
            string? reported_interval_description = twin.Properties.Reported["interval"].Contains("ad") ? twin.Properties.Reported["interval"]?["ad"] : "";
            int interval_value = desired_interval_value.Value;
            if (reported_interval_value != null)
            {
                interval_value = reported_interval_value.Value;
            }
            interval = new()
            {
                Value = interval_value,
                AckVersion = reported_interval_version ?? -1,
                AckDescription = reported_interval_description,
                AckStatus = reported_interval_status ?? -1,
                Version = twin.Properties.Desired.Version
            };
            return interval;
        }

        public async Task<enabled> Read_enabled_Property(string deviceId)
        {
            var twin = await rm.GetTwinAsync(deviceId);
            bool? desired_enabled_value = twin.Properties.Desired.Contains("enabled") ? twin.Properties.Desired["enabled"].Value : null;
            bool? reported_enabled_value = twin.Properties.Reported["enabled"]?["value"];
            int? reported_enabled_version = twin.Properties.Reported["enabled"]?["av"];
            int? reported_enabled_status = twin.Properties.Reported["enabled"]?["ac"];
            string? reported_enabled_description = twin.Properties.Reported["enabled"]?.Contains("ad") ? twin.Properties.Reported["enabled"]?["ad"] : "";
            bool enabled_value = desired_enabled_value ?? true;
            if (reported_enabled_value != null)
            {
                enabled_value = reported_enabled_value.Value;
            }
            enabled = new()
            {
                Value = enabled_value,
                AckVersion = reported_enabled_version ?? -1,
                AckDescription = reported_enabled_description,
                AckStatus = reported_enabled_status ?? -1,
                Version = twin.Properties.Desired.Version
            };
            return enabled;
        }


        public async Task<Twin> Update_interval(string deviceId, int interval)
        {
            var twin = await rm.GetTwinAsync(deviceId);
            var patch = new
            {
                properties = new
                {
                    desired = new
                    {
                        interval
                    }
                }
            };
            return await rm.UpdateTwinAsync(deviceId, js(patch), twin.ETag);
        }
        public async Task<Twin> Update_enabled(string deviceId, bool enabled)
        {
            var twin = await rm.GetTwinAsync(deviceId);
            var patch = new
            {
                properties = new
                {
                    desired = new
                    {
                        enabled
                    }
                }
            };
            return await rm.UpdateTwinAsync(deviceId, js(patch), twin.ETag);
        }
    }
}
