using Rido.IoTHubClient;

string js(object o) => System.Text.Json.JsonSerializer.Serialize(o);

ConnectionSettings cs = new ConnectionSettings(Environment.GetEnvironmentVariable("cs"));
(string u, string p) = SasAuth.GenerateHubSasCredentials(cs.HostName, cs.DeviceId, cs.SharedAccessKey, string.Empty, 60);



while (true)
{
    HttpClient http = new HttpClient();
    string urlTelemetry = $"https://{cs.HostName}/devices/{cs.DeviceId}/messages/events?api-version=2020-03-13";
    var reqTelemetry = new HttpRequestMessage(HttpMethod.Post, urlTelemetry)};
    reqTelemetry.Headers.Add("authorization", p);
    reqTelemetry.Content = new StringContent(js(new { Environment.WorkingSet }), System.Text.Encoding.UTF8, "application/json");
    var resp = await http.SendAsync(reqTelemetry);
    Console.WriteLine(resp.IsSuccessStatusCode);
    await Task.Delay(1000);
}