using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace twins_acks
{
    public class TwinProperties
    {
        JsonNode? root;
        public TwinProperties(string fullTwin)
        {
            root = JsonNode.Parse(fullTwin);
        }

        public int? DesiredVersion => root?["desired"]?["$version"]?.GetValue<int>();
        public int? ReportedVersion(string propName) => root?["reported"]?[propName]?["av"]?.GetValue<int>();


        public (bool? boolValue, bool needsAck) GetEffectiveProperty(string propName)
        {
            bool? result = null;
            bool needsAck = false;
            
            if (DesiredVersion == ReportedVersion(propName))
            {
                result = root?["desired"]?[propName]?.GetValue<bool>();
                needsAck = false;
            }

            if (DesiredVersion > ReportedVersion(propName))
            {
                result = root?["desired"]?[propName]?.GetValue<bool>();
                needsAck = true;
            }

            if (DesiredVersion < ReportedVersion(propName))
            {
                result = root?["reported"]?[propName]?["value"]?.GetValue<bool>();
                needsAck = false;
            }
            return (result, needsAck);
        }
    }
}
