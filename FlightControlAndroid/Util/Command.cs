using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace FlightControlAndroid.Util
{
    public class Command
    {
        [JsonPropertyName("throttle")]
        public double Throttle { set; get; }

        [JsonPropertyName("aileron")]
        public double Aileron{ set; get; }

        [JsonPropertyName("rudder")]
        public double Rudder{ set; get; }

        [JsonPropertyName("elevator")]
        public double Elevator { set; get; }
    }
}
