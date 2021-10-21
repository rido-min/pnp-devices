using Rido.IoTHubClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace thermostat_port
{
    internal class Program
    {
        private readonly Random _random = new();

        private double _temperature = 0d;
        private double _maxTemp = 0d;

        private readonly Dictionary<DateTimeOffset, double> _temperatureReadingsDateTimeOffset = new ();

        static async Task Main(string[] args) => await new Program().RunAsync();

        async Task RunAsync()
        {
            string idScope = "0ne00385995";
            string deviceId = "thermostat-port";
            string key = "2LfSC+WVObNbYE90dOioZ+rAVoWLoAjOW5IjxAZa9LQ=";

            //connectionString += ";ModelId=dtmi:com:example:Thermostat;1";


            var dpsRes = await DpsClient.ProvisionWithSasAsync(idScope, deviceId, key);
            var connectionString = $"HostName={dpsRes.registrationState.assignedHub};DeviceId={deviceId};SharedAccessKey={key};ModelId=dtmi:com:example:Thermostat;1";

            IHubMqttClient client = await HubMqttClient.CreateFromConnectionStringAsync(connectionString);
            Console.WriteLine(client.DeviceConnectionString);

            client.OnCommandReceived += async (o, c) =>
            {
                Console.WriteLine(c.CommandName);
                if (c.CommandName == "getMaxMinReport")
                {
                    Console.WriteLine("<- c: getMaxMinReport");

                    DateTime since = JsonSerializer.Deserialize<DateTime>(c.CommandRequestMessageJson);

                    Dictionary<DateTimeOffset, double> filteredReadings = _temperatureReadingsDateTimeOffset
                                            .Where(i => i.Key > since)
                                            .ToDictionary(i => i.Key, i => i.Value);

                    if (filteredReadings != null && filteredReadings.Any())
                    {
                        var report = new
                        {
                            maxTemp = filteredReadings.Values.Max<double>(),
                            minTemp = filteredReadings.Values.Min<double>(),
                            avgTemp = filteredReadings.Values.Average(),
                            startTime = filteredReadings.Keys.Min(),
                            endTime = filteredReadings.Keys.Max(),
                        };
                        await client.CommandResponseAsync(c.Rid, c.CommandRequestMessageJson, "200", report);
                    }
                }
            };


            client.OnPropertyReceived += async (o, p) =>
            {
                Console.WriteLine(p.PropertyMessageJson);
                var props = JsonDocument.Parse(p.PropertyMessageJson);
                if (props.RootElement.TryGetProperty("targetTemperature", out JsonElement prop))
                {
                    Console.WriteLine("<- w: targetTemperature ");
                    double targetTemperature = prop.GetDouble();
                    await client.UpdateTwinAsync(new { targetTemperature = new { ac = 202, av = p.Version, value = _temperature } });
                    double step = (targetTemperature - _temperature) / 2d;
                    for (int i = 1; i <= 2; i++)
                    {
                        _temperature = Math.Round(_temperature + step, 1);
                        await Task.Delay(6 * 1000);
                    }
                    await client.UpdateTwinAsync(new { targetTemperature = new { ac = 200, av = p.Version, value = _temperature } });
                }
            };

            while (true)
            {
                _temperature = Math.Round(_random.NextDouble() * 40.0 + 5.0, 1);
                _temperatureReadingsDateTimeOffset.Add(DateTimeOffset.Now, _temperature);

                double maxTemp = _temperatureReadingsDateTimeOffset.Values.Max<double>();

                if (maxTemp > _maxTemp)
                {
                    _maxTemp = maxTemp;
                    await client.UpdateTwinAsync(new { maxTempSinceLastReboot = maxTemp });
                    Console.WriteLine("-> r: maxTempSinceLastReboot");
                }
                await client.SendTelemetryAsync(new { temperature = _temperature });
                Console.WriteLine("-> t: temperature");
                await Task.Delay(5 * 1000);
            }
        }
    }
}
