//string hostname = "rido.azure-devices.net";
using dtmi_rido;

var cs = "HostName=tests.azure-devices.net;SharedAccessKeyName=iothubowner;SharedAccessKey=P5LfPNpLhLD/qJVOCTpuKXLi/9rmGqvkleB0quXxkws=";
string deviceId = "pnpbasic03";

pnp_basic_service_api sapi = new pnp_basic_service_api(cs);

var interval = await sapi.Read_interval_Property(deviceId);
Console.WriteLine($"interval: {interval.Value} ac:{interval.AckStatus} av:{interval.AckVersion} ad:{interval.AckDescription}");

var enabled = await sapi.Read_enabled_Property(deviceId);
Console.WriteLine($"enabled: {enabled.Value} ac:{enabled.AckStatus} av:{enabled.AckVersion} ad:{enabled.AckDescription}");

Cmd_getRuntimeStats_Response resp = await sapi.Invoke_getRuntimeStats_Async(
    deviceId, 
    new Cmd_getRuntimeStats_Request() 
        { 
            DiagnosticsMode = DiagnosticsMode.full
        });
Console.WriteLine();
resp.ToList().ForEach(x => Console.WriteLine($"{x.Key} {x.Value}"));


Console.WriteLine();
var started = await sapi.Read_started_Property(deviceId);
Console.WriteLine(started.Value);

var twin = await sapi.Update_interval(deviceId,interval.Value+1);

interval = await sapi.Read_interval_Property(deviceId);
Console.WriteLine($"interval: {interval.Value} ac:{interval.AckStatus} av:{interval.AckVersion} ad:{interval.AckDescription}");

enabled = await sapi.Read_enabled_Property(deviceId);
Console.WriteLine($"enabled: {enabled.Value} ac:{enabled.AckStatus} av:{enabled.AckVersion} ad:{enabled.AckDescription}");





















//var dtwinResp = await dt.GetDigitalTwinAsync<BasicDigitalTwin>(deviceId);
//dtwinResp.Body.CustomProperties.ToList().ForEach(p => Console.WriteLine($"{p.Key} {p.Value}"));


Console.ReadLine();

//var query = registryManager.CreateQuery("select * from Devices", 100);
//while (query.HasMoreResults)
//{
//    var resp = await query.GetNextAsTwinAsync();
//    resp.ToList().ForEach(x => Console.WriteLine(x.DeviceId));
//}


