using Newtonsoft.Json;

namespace Lively.Models.LivelyControls
{
    public class SliderModel : ControlModel
    {
        [JsonIgnore]
        [JsonProperty("tick")]
        public int Tick { get; set; }

        [JsonProperty("min")]
        public double Min { get; set; }

        [JsonProperty("max")]
        public double Max { get; set; }

        [JsonProperty("value")]
        public double Value { get; set; }

        // Default value 1, otherwise if missing it will be 0 and crash on moving slider.
        [JsonProperty("step")]
        public double Step { get; set; } = 1f;

        public SliderModel() : base("slider") { }
    }
}
