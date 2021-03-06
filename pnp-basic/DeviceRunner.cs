using System.Text;
using System.Diagnostics;
using Rido.IoTHubClient;
using dtmi_rido;
using Humanizer;

namespace pnp_basic;

public class DeviceRunner : BackgroundService
{
    private readonly ILogger<DeviceRunner> _logger;
    private readonly IConfiguration _configuration;

    Timer? screenRefresher;
    readonly Stopwatch clock = Stopwatch.StartNew();

    double telemetryWorkingSet = 0;

    int telemetryCounter = 0;
    int commandCounter = 0;
    int twinCounter = 0;
    int reconnectCounter = 0;

    dtmi_rido.pnp_basic? client;

    const bool default_enabled = true;
    const int default_interval = 234;

    public DeviceRunner(ILogger<DeviceRunner> logger, IConfiguration configuration)
    {
        _logger = logger;
        _configuration = configuration;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        Console.WriteLine("Connecting..");
        client = await dtmi_rido.pnp_basic.CreateDeviceClientAsync(_configuration.GetConnectionString("hub")) ?? 
            throw new ApplicationException("Error creating MQTT Client");

        client._connection.OnMqttClientDisconnected += (o,e) => reconnectCounter++;
        
        client.OnProperty_enabled_Updated = Property_enabled_UpdateHandler;
        client.OnProperty_interval_Updated = Property_interval_UpdateHandler;
        client.OnCommand_getRuntimeStats_Invoked = Command_getRuntimeStats_Handler;

        _ = await client.Report_started_Async(DateTime.Now);
            
        await client.InitProperty_interval_Async(default_interval);
        await client.InitProperty_enabled_Async(default_enabled);
               
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
    
    async Task<WritableProperty<bool>> Property_enabled_UpdateHandler(WritableProperty<bool> req)
    {
        twinCounter++;
        var ack = new WritableProperty<bool>("enabled")
        {
            Description = "desired notification accepted",
            Status = 200,
            Version = Convert.ToInt32(req?.Version),
            Value = req?.Value
        };
        return await Task.FromResult(ack);
    }

    async Task<WritableProperty<int>> Property_interval_UpdateHandler(WritableProperty<int> req)
    {
        twinCounter++;
        var ack = new WritableProperty<int>("interval")
        {
            Description = (client?.Property_enabled?.Value == true) ? "desired notification accepted" : "disabled, not accepted",
            Status = (client?.Property_enabled?.Value == true) ? 200 : 205,
            Version = Convert.ToInt32(req?.Version),
            Value = req?.Value ?? 0
        };
        return await Task.FromResult(ack);
    }


    async Task<Cmd_getRuntimeStats_Response> Command_getRuntimeStats_Handler(Cmd_getRuntimeStats_Request req)
    {
        commandCounter++;
        var result = new Cmd_getRuntimeStats_Response()
        {
            Status = 200
        };

        //result.Add("runtime version", System.Reflection.Assembly.GetEntryAssembly()?.GetCustomAttribute<System.Runtime.Versioning.TargetFrameworkAttribute>()?.FrameworkName ?? "n/a");
        result.Add("machine name", Environment.MachineName);
        result.Add("os version", Environment.OSVersion.ToString());
        if (req.DiagnosticsMode == DiagnosticsMode.complete)
        {
            result.Add("this app:", System.Reflection.Assembly.GetExecutingAssembly()?.FullName ?? "");
        }
        if (req.DiagnosticsMode == DiagnosticsMode.full)
        {
            result.Add($"twin counter: ",  twinCounter.ToString());
            result.Add("telemetry counter: ", telemetryCounter.ToString());
            result.Add("command counter: ", commandCounter.ToString());
            result.Add("reconnects counter: ", reconnectCounter.ToString());
        }
        return await Task.FromResult(result);
    }

    private void RefreshScreen(object? state)
    {
        string RenderData()
        {
            string? enabled_value = client?.Property_enabled?.Value?.ToString();
            string? interval_value = client?.Property_interval?.Value?.ToString();
            StringBuilder sb = new ();
            sb.AppendLine(client?._connection.ConnectionSettings.ToString());
            sb.AppendLine("");
            sb.AppendLine(String.Format("{0:8} | {1:5} | {2}", "Property", "Value", "Version"));
            sb.AppendLine(String.Format("{0:8} | {1:5} | {2}", "--------", "-----", "------"));
            sb.AppendLine(String.Format("{0:8} | {1:5} | {2}", "enabled".PadRight(8), enabled_value?.PadLeft(5), client?.Property_enabled?.Version));
            sb.AppendLine(String.Format("{0:8} | {1:5} | {2}", "interval".PadRight(8), interval_value?.PadLeft(5), client?.Property_interval?.Version));
            sb.AppendLine("");
            sb.AppendLine($"Reconnects: {reconnectCounter}");
            sb.AppendLine($"Telemetry messages: {telemetryCounter}");
            sb.AppendLine($"Twin messages: {twinCounter}");
            sb.AppendLine($"Command messages: {commandCounter}");
            sb.AppendLine("");
            sb.AppendLine($"WorkingSet: {telemetryWorkingSet.Bytes()}");
            sb.AppendLine("");
            sb.Append($"Time Running: {TimeSpan.FromMilliseconds(clock.ElapsedMilliseconds).Humanize(3)}");
            return sb.ToString();
        }

        Console.SetCursorPosition(0, 0);
        Console.WriteLine(RenderData());
        screenRefresher = new Timer(RefreshScreen, this, 1000, 0);
    }
}
