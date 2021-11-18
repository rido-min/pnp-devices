//string hostname = "rido.azure-devices.net";
using Microsoft.Azure.Devices.Serialization;

string js(object o) => System.Text.Json.JsonSerializer.Serialize(o);
var cs = "HostName=rido.azure-devices.net;SharedAccessKeyName=iothubowner;SharedAccessKey=laa2Ul6M1YOYATW9aOHxdimAJjHTmVp1vXIu8csa70U=";
string deviceId = "t1";
var serviceClient = Microsoft.Azure.Devices.ServiceClient.CreateFromConnectionString(cs);
var registryManager = Microsoft.Azure.Devices.RegistryManager.CreateFromConnectionString(cs);
var dt = Microsoft.Azure.Devices.DigitalTwinClient.CreateFromConnectionString(cs);

var dtwinResp = await dt.GetDigitalTwinAsync<BasicDigitalTwin>(deviceId);
dtwinResp.Body.CustomProperties.ToList().ForEach(p => Console.WriteLine($"{p.Key} {p.Value}"));

async Task<Microsoft.Azure.Devices.Shared.Twin> UpdateIntervalAsync(string deviceId, int interval)
{
    ArgumentNullException.ThrowIfNull(registryManager);
    var twin = await registryManager.GetTwinAsync(deviceId);
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
    return await registryManager.UpdateTwinAsync(deviceId, js(patch), twin.ETag);
}

_ = await UpdateIntervalAsync(deviceId, 33);

Console.ReadLine();

//var query = registryManager.CreateQuery("select * from Devices", 100);
//while (query.HasMoreResults)
//{
//    var resp = await query.GetNextAsTwinAsync();
//    resp.ToList().ForEach(x => Console.WriteLine(x.DeviceId));
//}


