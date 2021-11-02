using Rido.IoTHubClient;
using System;
using System.Threading.Tasks;

namespace telemetry_samples
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var client = await HubMqttClient.CreateFromConnectionStringAsync(Environment.GetEnvironmentVariable("dps"));
            
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
