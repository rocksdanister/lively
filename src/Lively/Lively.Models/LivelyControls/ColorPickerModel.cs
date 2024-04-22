using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lively.Models.LivelyControls
{
    public class ColorPickerModel : ControlModel
    {
        [JsonProperty("value")]
        public string Value { get; set; }

        public ColorPickerModel() : base("color") { }
    }
}
