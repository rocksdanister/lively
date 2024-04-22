using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
