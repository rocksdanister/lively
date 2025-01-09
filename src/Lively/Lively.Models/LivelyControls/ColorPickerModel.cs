using Newtonsoft.Json;

namespace Lively.Models.LivelyControls
{
    public class ColorPickerModel : ControlModel
    {
        [JsonProperty("value")]
        public string Value { get; set; }

        public ColorPickerModel() : base("color") { }
    }
}
