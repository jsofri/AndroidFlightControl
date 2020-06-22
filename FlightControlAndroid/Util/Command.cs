using System.Text.Json.Serialization;

namespace FlightControlAndroid.Util
{
    public class Command
    {
        [JsonPropertyName("throttle")]
        public double Throttle { get; set; }

        [JsonPropertyName("aileron")]
        public double Aileron { get; set; }

        [JsonPropertyName("rudder")]
        public double Rudder { get; set; }

        [JsonPropertyName("elevator")]
        public double Elevator { get; set;}
    }
}
