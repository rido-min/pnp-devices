using dtmi_rido;
using Humanizer;
using Rido.IoTHubClient;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.Versioning;
using System.Text;

namespace pnp_basic;

public class DeviceRunner : BackgroundService
{
    Stopwatch clock = Stopwatch.StartNew();

    dtmi_rido.pnp_basic? client;
    private readonly ILogger<DeviceRunner> _logger;
    private readonly IConfiguration _configuration;

    const bool default_enabled = true;
    const int default_interval = 2;

    Timer? screenRefresher;

    double telemetryWorkingSet = 0;

    int telemetryCounter = 0;
    int commandCounter = 0;
    int twinCounter = 0;

    public DeviceRunner(ILogger<DeviceRunner> logger, IConfiguration configuration)
    {
        _logger = logger;
        _configuration = configuration;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        client = await dtmi_rido.pnp_basic.CreateDeviceClientAsync(_configuration.GetConnectionString("hub"));
        client.OnProperty_enabled_Updated = Property_enabled_UpdateHandler;
        client.OnProperty_interval_Updated = Property_interval_UpdateHandler;
        client.OnCommand_getRuntimeStats_Invoked = getRuntimeStats;
        await client.InitTwinAsync(default_enabled, default_interval);

        await client.Report_started_Async(DateTime.Now);

        Console.Clear();
        screenRefresher = new Timer(RefreshScreen, this, 1000, 0);

        while (!stoppingToken.IsCancellationRequested)
        {
            if (client?.Property_enabled?.Value == true)
            {
                telemetryWorkingSet = Environment.WorkingSet;
                await client.Send_workingSet_Async(telemetryWorkingSet);
                telemetryCounter++;
            }
            var interval = client?.Property_interval?.Value;
            await Task.Delay(interval.HasValue ? interval.Value * 1000 : 1000, stoppingToken);
        }
    }

    async Task<WritableProperty<int>> Property_interval_UpdateHandler(WritableProperty<int> req)
    {
        twinCounter++;
        var ack = new WritableProperty<int>("interval")
        {
            Description = (client?.Property_enabled?.Value == true) ? "desired notification accepted" : "disabled, not accepted",
            Status = (client?.Property_enabled?.Value == true) ? 200 : 304,
            Version = Convert.ToInt32(req?.Version),
            Value = req?.Value ?? 0
        };
        return await Task.FromResult(ack);
    }

    async Task<WritableProperty<bool>> Property_enabled_UpdateHandler(WritableProperty<bool> req)
    {
        twinCounter++;
        var ack = new WritableProperty<bool>("enabled")
        {
            Description = (client?.Property_enabled?.Value == true) ? "desired notification accepted" : "disabled, not accepted",
            Status = (client?.Property_enabled?.Value == true) ? 200 : 304,
            Version = Convert.ToInt32(req?.Version),
            Value = req?.Value ?? false
        };
        return await Task.FromResult(ack);
    }

    async Task<Cmd_getRuntimeStats_Response> getRuntimeStats(Cmd_getRuntimeStats_Request req)
    {
        commandCounter++;
        var result = new Cmd_getRuntimeStats_Response()
        {
            _status = 200,
            _rid = req._rid
        };

        result.Add("runtime version", Assembly.GetEntryAssembly()?.GetCustomAttribute<TargetFrameworkAttribute>()?.FrameworkName ?? "n/a");
        result.Add("machine name", Environment.MachineName);
        if (req.DiagnosticsMode == DiagnosticsMode.full)
        {
            result.Add("this app:", Assembly.GetExecutingAssembly()?.FullName ?? "");
            result.Add("os version", Environment.OSVersion.ToString());
            result.Add($"twin counter: ",  twinCounter.ToString());
            result.Add("telemetry counter: ", telemetryCounter.ToString());
            result.Add("command counter: ", commandCounter.ToString());
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
            sb.AppendLine($"enabled:  {client?.Property_enabled?.Value}  \tversion: {client?.Property_enabled?.Version}             ");
            sb.AppendLine($"interval: {client?.Property_interval?.Value} \t\tversion: {client?.Property_interval?.Version}            ");
            sb.AppendLine("");
            sb.AppendLine($"Telemetry messages: {telemetryCounter}");
            sb.AppendLine($"Twin messages: {twinCounter}");
            sb.AppendLine($"Command messages: {commandCounter}");
            sb.AppendLine("");
            sb.AppendLine($"WorkingSet: {telemetryWorkingSet.Bytes()}");
            sb.AppendLine("");
            sb.Append($"Time Running: {TimeSpan.FromMilliseconds(clock.ElapsedMilliseconds).Humanize(4)}");
            return sb.ToString();
        }

        Console.SetCursorPosition(0, 0);
        Console.WriteLine(RenderData());
        screenRefresher = new Timer(RefreshScreen, this, 1000, 0);
    }
}
