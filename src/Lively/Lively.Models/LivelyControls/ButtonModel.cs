using Newtonsoft.Json;

namespace Lively.Models.LivelyControls
{
    public class ButtonModel : ControlModel
    {
        [JsonProperty("value")]
        public string Value { get; set; }

        public ButtonModel() : base("button") { }
    }
}
