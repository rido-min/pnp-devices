using Rido.IoTHubClient;
using System;
using System.Threading.Tasks;

namespace telemetry_samples
{
    class Program
    {
        static async Task Main()
        {
            var client = await HubMqttClient.CreateAsync(ConnectionSettings.FromConnectionString(Environment.GetEnvironmentVariable("dps")));
            
            int i = 0;
            while (true)
            {
                await client.SendTelemetryAsync(new { Sinusoid = Math.Sin(i++), Triangle = i % 5 });
                await client.SendTelemetryAsync(new { Square = Math.Sqrt(i) }, "Math");
                await Task.Delay(1000);
            }    
        }
    }
}
