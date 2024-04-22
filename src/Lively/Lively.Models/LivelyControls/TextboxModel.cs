using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lively.Models.LivelyControls
{
    public class TextboxModel : ControlModel
    {
        [JsonProperty("value")]
        public string Value { get; set; }

        public TextboxModel() : base("textbox") { }
    }
}
