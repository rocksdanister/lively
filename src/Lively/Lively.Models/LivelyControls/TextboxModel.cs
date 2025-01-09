using Newtonsoft.Json;

namespace Lively.Models.LivelyControls
{
    public class TextboxModel : ControlModel
    {
        [JsonProperty("value")]
        public string Value { get; set; }

        public TextboxModel() : base("textbox") { }
    }
}
