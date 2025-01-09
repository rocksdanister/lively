using Newtonsoft.Json;

namespace Lively.Models.LivelyControls
{
    public class FolderDropdownModel : ControlModel
    {
        [JsonProperty("value")]
        public string Value { get; set; }

        [JsonProperty("folder")]
        public string Folder { get; set; }

        [JsonProperty("filter")]
        public string Filter { get; set; }

        public FolderDropdownModel() : base("folderDropdown") { }
    }
}
