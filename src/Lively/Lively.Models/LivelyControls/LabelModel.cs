using Newtonsoft.Json;

namespace Lively.Models.LivelyControls
{
    public class LabelModel : ControlModel
    {
        [JsonProperty("value")]
        public string Value { get; set; }

        public LabelModel() : base("label") { }
    }
}
