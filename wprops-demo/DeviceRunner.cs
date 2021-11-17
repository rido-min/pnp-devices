using rido.wprops_demo;
using Rido.IoTHubClient;
using System.Reflection;
using System.Runtime.Versioning;
using System.Text;
using System.Text.Json;

namespace wprops_demo
{
    public class DeviceRunner : BackgroundService
    {
        DeviceClient? client;
        private readonly ILogger<DeviceRunner> _logger;
        private readonly IConfiguration _configuration;

        const bool default_enabled = true;
        const int default_interval = 5;

        Timer? screenRefresher;

        public DeviceRunner(ILogger<DeviceRunner> logger, IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;
            
        }

       

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            client = await DeviceClient.CreateDeviceClientAsync(_configuration.GetConnectionString("hub"));
            
            client.OnProperty_enabled_Updated = async req =>
            {
                var ack = new WritableProperty<bool>("enabled")
                {
                    Description = "desired notification accepted",
                    Status = 200,
                    Version = Convert.ToInt32(req?.Version),
                    Value = Convert.ToBoolean(req?.Value)
                };
                return await Task.FromResult(ack);
            };
            client.OnProperty_interval_Updated = async req =>
            {
                var ack =  new WritableProperty<int>("interval")
                {
                    Description = "desired notification accepted",
                    Status = 200,
                    Version = Convert.ToInt32(req?.Version),
                    Value = Convert.ToInt32(req?.Value)
                };
                return await Task.FromResult(ack);
            };
            
            client.OnCommand_getRuntimeStats_Invoked = getRuntimeStats;

            await client.InitTwinAsync(default_enabled, default_interval);

            await client.Report_started_Async(DateTime.Now);

            Console.Clear();
            screenRefresher = new Timer(RefreshScreen, this, 1000, 0);
            while (!stoppingToken.IsCancellationRequested)
            {
                await client.Send_workingSet_Async(Environment.WorkingSet);
                //_logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);
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

        private void RefreshScreen(object? state)
        {
            string RenderData()
            {
                StringBuilder sb = new StringBuilder();
                sb.AppendLine(client?._connection.ConnectionSettings.ToString());
                sb.AppendLine("");
                sb.AppendLine($"enabled: {client?.Property_enabled?.Value}  version: {client?.Property_enabled?.Version}             ");
                sb.AppendLine($"interval: {client?.Property_interval?.Value} version: {client?.Property_interval?.Version}           ");
                //sb.AppendLine(client.ConnectionSettings.ToString());
                sb.AppendLine("");
                sb.AppendLine("");
                sb.AppendLine("");
                sb.AppendLine($"WorkingSet: {Environment.WorkingSet}");
                //sb.Append($"Time Running: {TimeSpan.FromMilliseconds(clock.ElapsedMilliseconds).Humanize(4)}");
                return sb.ToString();
            }

            Console.SetCursorPosition(0, 0);
            Console.WriteLine(RenderData());
            screenRefresher = new Timer(RefreshScreen, this, 1000, 0);
        }

        
    }
}