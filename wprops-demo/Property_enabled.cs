using System.Text.Json;

namespace rido.wprops_demo
{
    public class WritableProperty<T>
    {
        public int? Version { get; set; }
        public string? Description { get; set; }
        public int? Status { get; set; }
        public T? Value { get; set; }

        internal string ToAck()
        {
            return JsonSerializer.Serialize(new 
            { 
                ac = Status,
                av = Version,
                value = Value,
                ad = Description
            });
        }
    }

}