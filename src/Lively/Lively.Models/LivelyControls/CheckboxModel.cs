using Newtonsoft.Json;

namespace Lively.Models.LivelyControls
{
    public class CheckboxModel : ControlModel
    {
        [JsonProperty("value")]
        public bool Value { get; set; }

        public CheckboxModel() : base("checkbox") { }
    }
}
