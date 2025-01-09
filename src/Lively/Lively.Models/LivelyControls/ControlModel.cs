using Newtonsoft.Json;

namespace Lively.Models.LivelyControls
{
    public class ControlModel
    {
        [JsonIgnore]
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("text")]
        public string Text { get; set; }

        [JsonProperty("type")]
        public string Type { get; }

        [JsonProperty("help")]
        public string Help { get; set; }

        protected ControlModel(string Type)
        {
            this.Type = Type;
        }
    }
}
