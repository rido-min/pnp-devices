using Rido.IoTHubClient;
using System.Text.Json.Nodes;
using user_interaction;

static string js(object o) => System.Text.Json.JsonSerializer.Serialize(o);

var cs = "HostName=tests.azure-devices.net;DeviceId=usertests;SharedAccessKey=MDAwMDAwMDAwMDAwMDAwMDAwMDAwMDAwMDAwMDAwMDA=";
var client = await HubMqttClient.CreateAsync(cs);
Console.WriteLine(client.ConnectionSettings);
var twinJson = await client.GetTwinAsync();

string? myWritableProp = WritablePropertiesHelper.ReadProperty(nameof(myWritableProp), twinJson);

await client.UpdateTwinAsync(new PropertyAck()
{
    Status = 200,
    Version =  desiredVersion ?? 0,
    Description = "Updated to default value",
    Value = js(new { myWritableProp })

}.BuildAck());

twinJson = await client.GetTwinAsync();
Console.WriteLine(twinJson);

Console.WriteLine("Set value from user");
myWritableProp = Console.ReadLine();

await client.UpdateTwinAsync(new PropertyAck()
{
    Status = 200,
    Version = desiredVersion ?? 0,
    Description = "Updated from user value",
    Value = js(new { myWritableProp })

}.BuildAck());

twinJson = await client.GetTwinAsync();
Console.WriteLine(twinJson);

client.OnPropertyChange = async p =>
{
    System.Console.WriteLine("received property update");
    return await Task.FromResult(new PropertyAck()
    {
        Version = p.Version,
        Description = "received from service",
        Status = 200,
        Value = p.PropertyMessageJson

    });
};

Console.ReadLine();