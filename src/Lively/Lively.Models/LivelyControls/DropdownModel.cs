using Newtonsoft.Json;

namespace Lively.Models.LivelyControls
{
    public class DropdownModel : ControlModel
    {
        [JsonProperty("value")]
        public int Value { get; set; }

        [JsonProperty("items")]
        public string[] Items { get; set; }

        public DropdownModel() : base("dropdown") { }
    }
}
