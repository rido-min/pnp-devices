﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Rido.IoTHubClient.HiveTopicBinders
{
    public class TelemetryBinder
    {
        readonly IMqttConnection connection;
        readonly string deviceId;
        readonly string component;
        public TelemetryBinder(IMqttConnection connection, string component = "")
        {
            this.connection = connection;
            this.deviceId = connection.ConnectionSettings.DeviceId;
            this.component = component;
        }
        public async Task<PubResult> SendTelemetryAsync(object payload, CancellationToken cancellationToken = default)
        {
            string topic = $"pnp/{deviceId}/telemetry";
            var pubAck = await connection.PublishAsync(topic, payload, cancellationToken);
            var pubResult = (PubResult)pubAck.ReasonCode;
            return pubResult;
        }
    }
}
