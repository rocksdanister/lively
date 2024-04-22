using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lively.Models.LivelyControls
{
    public class CheckboxModel : ControlModel
    {
        [JsonProperty("value")]
        public bool Value { get; set; }

        public CheckboxModel() : base("checkbox") { }
    }
}
