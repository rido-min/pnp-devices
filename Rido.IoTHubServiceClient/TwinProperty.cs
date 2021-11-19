using System;

namespace Rido.IoTHubServiceClient
{
    public class TwinProperty<T>
    {
        public long Version { get; set; }
        public DateTime LastUpdated { get; set; }
        public int LastUpdatedVersion { get; set; }
        public T? Value { get; set; }
    }
}
