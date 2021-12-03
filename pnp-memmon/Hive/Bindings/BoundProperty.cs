using System;
using System.Threading.Tasks;

namespace Rido.IoTHubClient.HiveTopicBinders
{
    public class Bound_Property<T>
    {
        public WritableProperty<T> PropertyValue;
        readonly string propertyName;
        readonly DesiredUpdatePropertyBinder<T> desiredBinder;
        UpdatePropertyBinder updatePropertyBinder;

        public Func<WritableProperty<T>, Task<WritableProperty<T>>> OnProperty_Updated
        {
            set => desiredBinder.OnProperty_Updated = value;
        }

        public Bound_Property(IMqttConnection connection, string name)
        {
            propertyName = name;
            updatePropertyBinder = new UpdatePropertyBinder(connection);
            PropertyValue = new WritableProperty<T>(name);
            desiredBinder = new DesiredUpdatePropertyBinder<T>(connection, name);
            //updateTwin = new UpdateTwinBinder(connection);
        }

        //public async Task UpdateTwinAsync() => await updateTwin.SendRequestWaitForResponse(this.PropertyValue.ToAck());

        public async Task InitPropertyAsync(string twin, T defaultValue)
        {
            PropertyValue = new WritableProperty<T>(propertyName) { Value = defaultValue };
            await updatePropertyBinder.SendRequestWaitForResponse(PropertyValue.ToAck());

            //PropertyValue = WritableProperty<T>.InitFromTwin(twin, propertyName, defaultValue);
            //if (desiredBinder.OnProperty_Updated != null && (PropertyValue.DesiredVersion > 1))
            //{
            //    var ack = await desiredBinder.OnProperty_Updated.Invoke(PropertyValue);
            //    _ = updateTwin.SendRequestWaitForResponse(ack.ToAck());
            //    PropertyValue = ack;
            //}
            //else
            //{
            //    _ = updateTwin.SendRequestWaitForResponse(PropertyValue.ToAck());
            //}
        }
    }
}
