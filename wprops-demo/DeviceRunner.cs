using rido.wprops_demo;
using Rido.IoTHubClient;
using System.Reflection;
using System.Runtime.Versioning;
using System.Text.Json;

namespace wprops_demo
{
    public class DeviceRunner : BackgroundService
    {

        private readonly ILogger<DeviceRunner> _logger;
        private readonly IConfiguration _configuration;

        const bool default_enabled = true;
        const int default_interval = 5;

        bool enabled;
        int interval;

        public DeviceRunner(ILogger<DeviceRunner> logger, IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {

            var client = await DeviceClient.CreateDeviceClientAsync(_configuration.GetConnectionString("hub"));
            Console.WriteLine(client._connection.ConnectionSettings.ToString());
            client.OnProperty_enabled_Updated = async req =>
            {
                Console.WriteLine("Received enabled");
                enabled = req.Value;
                var ack = new WritableProperty<bool>("enabled")
                {
                    Description = "desired notification accepted",
                    Status = 200,
                    Version = Convert.ToInt32(req?.Version),
                    Value = enabled
                };
                return await Task.FromResult(ack);
            };
            client.OnProperty_interval_Updated = async req =>
            {
                Console.WriteLine("Received interval");
                interval = req.Value;
                var ack =  new WritableProperty<int>("interval")
                {
                    Description = "desired notification accepted",
                    Status = 200,
                    Version = Convert.ToInt32(req?.Version),
                    Value = interval
                };
                return await Task.FromResult(ack);
            };
            client.OnCommand_getRuntimeStats_Invoked = getRuntimeStats;
            await client.InitTwinAsync();
            await client.Report_started_Async(DateTime.Now);
            


            while (!stoppingToken.IsCancellationRequested)
            {
                await client.Send_workingSet_Async(Environment.WorkingSet);
                _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);
                await Task.Delay(5000, stoppingToken);
            }
        }

        async Task<Cmd_getRuntimeStats_Response> getRuntimeStats(Cmd_getRuntimeStats_Request req)
        {
            var result = new Cmd_getRuntimeStats_Response()
            {
                _status = 200,
                _rid = req._rid
            };

            result.Add("runtime version", Assembly.GetEntryAssembly()?.GetCustomAttribute<TargetFrameworkAttribute>()?.FrameworkName ?? "n/a");
            result.Add("machine name", Environment.MachineName);
            if (req.DiagnosticsMode == DiagnosticsMode.full)
            {
                result.Add("os version", Environment.OSVersion.ToString());
            }

            return await Task.FromResult(result);

        }
    }
}