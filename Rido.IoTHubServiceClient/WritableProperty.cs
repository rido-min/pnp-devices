namespace Rido.IoTHubServiceClient
{
    public class WritableProperty<T> : TwinProperty<T>
    {
        public int AckStatus { get; set; }
        public int AckVersion { get; set; }
        public string? AckDescription { get; set; }

    }
}
