string js(object o) => System.Text.Json.JsonSerializer.Serialize(o);

var cs = Rido.IoTHubClient.ConnectionSettings.FromConnectionString(Environment.GetEnvironmentVariable("cs"));
string urlTelemetry = $"https://{cs.HostName}/devices/{cs.DeviceId}/messages/events?api-version=2020-03-13";

while (true)
{
    (_, string token) = Rido.IoTHubClient.SasAuth.GenerateHubSasCredentials(cs.HostName, cs.DeviceId, cs.SharedAccessKey, "");
    var resp = await new HttpClient()
                        .SendAsync(
                            new HttpRequestMessage(
                                HttpMethod.Post, 
                                urlTelemetry)
                                {
                                    Headers = { { "authorization", token } },
                                    Content = new StringContent(
                                        js(new { Environment.WorkingSet }), 
                                        System.Text.Encoding.UTF8, 
                                        "application/json")
                                });
    Console.WriteLine(resp.IsSuccessStatusCode);
    await Task.Delay(1000);
}