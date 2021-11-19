//string hostname = "rido.azure-devices.net";
using dtmi_rido;

string deviceId = "pnpbasic-local";
pnp_basic_service_api sapi = new pnp_basic_service_api(Environment.GetEnvironmentVariable("cs"));

var started = await sapi.Read_started_Property(deviceId);
Console.WriteLine($"device: {deviceId} started {started.Value} v:{started.Version}");
Console.WriteLine();

var interval = await sapi.Read_interval_Property(deviceId);
Console.WriteLine($"interval: {interval.Value} ac:{interval.AckStatus} av:{interval.AckVersion} ad:{interval.AckDescription}");

var enabled = await sapi.Read_enabled_Property(deviceId);
Console.WriteLine($"enabled: {enabled.Value} ac:{enabled.AckStatus} av:{enabled.AckVersion} ad:{enabled.AckDescription}");

Console.Write("\nupdating interval");
var twin = await sapi.Update_interval(deviceId, interval.Value+1);
Console.WriteLine(".. interval updated\n");

interval = await sapi.Read_interval_Property(deviceId);
Console.WriteLine($"interval: {interval.Value} ac:{interval.AckStatus} av:{interval.AckVersion} ad:{interval.AckDescription}");

enabled = await sapi.Read_enabled_Property(deviceId);
Console.WriteLine($"enabled: {enabled.Value} ac:{enabled.AckStatus} av:{enabled.AckVersion} ad:{enabled.AckDescription}");

Console.Write("\nInvoking getRuntimeStats");
Cmd_getRuntimeStats_Response resp = await sapi.Invoke_getRuntimeStats_Async(
    deviceId,
    new Cmd_getRuntimeStats_Request()
    {
        DiagnosticsMode = DiagnosticsMode.full
    });
Console.WriteLine();
resp.ToList().ForEach(x => Console.WriteLine($"{x.Key} {x.Value}"));


















//var dtwinResp = await dt.GetDigitalTwinAsync<BasicDigitalTwin>(deviceId);
//dtwinResp.Body.CustomProperties.ToList().ForEach(p => Console.WriteLine($"{p.Key} {p.Value}"));


Console.ReadLine();

//var query = registryManager.CreateQuery("select * from Devices", 100);
//while (query.HasMoreResults)
//{
//    var resp = await query.GetNextAsTwinAsync();
//    resp.ToList().ForEach(x => Console.WriteLine(x.DeviceId));
//}


