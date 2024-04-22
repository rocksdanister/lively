using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
