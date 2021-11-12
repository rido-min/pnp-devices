using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

namespace user_interaction
{
    internal class WritablePropertiesHelper
    {
        internal static string? ReadProperty(string propName, string twinString)
        {
            string? prop = null;
            var twin = JsonNode.Parse(twinString);
            string? desiredValue = string.Empty;
            int? desiredVersion = null;
            string? reportedValue = string.Empty;
            int? reportedVersion = null;

            if (twin?["desired"]?[propName] != null)
            {
                desiredValue = twin?["desired"]?[propName]?.ToString();
                desiredVersion = Convert.ToInt32(twin?["desired"]?["$version"]?.ToString());
            }

            if (twin?["reported"]?[propName] != null)
            {
                reportedValue = twin?["reported"]?[propName]?["value"]?.ToString();
                reportedVersion = Convert.ToInt32(twin?["reported"]?[propName]?["av"]?.ToString());
            }

            if (desiredVersion == reportedVersion)
            {
                prop = desiredValue;
            }
            else
            {
                if (desiredVersion > reportedVersion)
                {
                    //should send ack

                }
            }

            return prop;
        }
    }
}
