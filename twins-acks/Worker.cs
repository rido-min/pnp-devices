using Rido.IoTHubClient;

namespace twins_acks
{
    public class Worker : BackgroundService
    {
        static string js(object o) => System.Text.Json.JsonSerializer.Serialize(o);

        private readonly ILogger<Worker> _logger;
        IConfiguration _configuration;

        public Worker(ILogger<Worker> logger, IConfiguration config)
        {
            _logger = logger;
            _configuration = config;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var cs = _configuration.GetConnectionString("dps");
            var client = await HubMqttClient.CreateAsync(new ConnectionSettings(cs) 
            {
                ModelId = "dtmi:rido:onewprop;1"
            });
            _logger.LogWarning($"{client.ConnectionSettings}");

            //reset
            //await client.UpdateTwinAsync("{ \"enabled\" : null }");

            var twin = await client.GetTwinAsync();
            _logger.LogInformation(twin);

            const bool DEFAULT_ENABLED_VALUE = true;

            var tp = new TwinProperties(twin);
            (bool? effectiveValue, bool needsAck) = tp.GetEffectiveProperty("enabled");

            if (effectiveValue.HasValue)
            {
                Console.WriteLine("Enabled property value: " + effectiveValue);
            }
            else
            {
                Console.WriteLine("Enabled property value not set");
                _ = await client.UpdateTwinAsync(new PropertyAck()
                {
                    Description = "Updating ACK with default value",
                    Status = 200,
                    Version = 0,
                    Value = js(new { enabled = DEFAULT_ENABLED_VALUE })
                }.BuildAck());
            }

            if (needsAck)
            {
                _ = await client.UpdateTwinAsync(new PropertyAck()
                {
                    Description = "Updating ACK from startup",
                    Status = 200,
                    Version = tp.DesiredVersion.Value,
                    Value = js(new { enabled = effectiveValue })
                }.BuildAck());
            }


            client.OnPropertyChange = async p =>
            {
                _logger.LogInformation($"Received prop version: {p.Version}");
                await Task.Delay(500);
                return new PropertyAck
                {
                    Description = "property accepted",
                    Status = 200,
                    Version = p.Version,
                    Value = p.PropertyMessageJson
                };
            };

            
            while (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);
                await Task.Delay(1000, stoppingToken);
            }
        }
    }
}