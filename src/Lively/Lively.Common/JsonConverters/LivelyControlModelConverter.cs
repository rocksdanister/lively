using Lively.Models.LivelyControls;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lively.Common.JsonConverters
{
    public class LivelyControlModelConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return (objectType == typeof(ControlModel));
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            var jsonObject = JObject.Load(reader);
            var type = jsonObject["type"]?.Value<string>();

            ControlModel control = type?.ToLower() switch
            {
                "slider" => jsonObject.ToObject<SliderModel>(),
                "textbox" => jsonObject.ToObject<TextboxModel>(),
                "dropdown" => jsonObject.ToObject<DropdownModel>(),
                "folderdropdown" => jsonObject.ToObject<FolderDropdownModel>(),
                "scalerdropdown" => jsonObject.ToObject<ScalerDropdownModel>(),
                "button" => jsonObject.ToObject<ButtonModel>(),
                "color" => jsonObject.ToObject<ColorPickerModel>(),
                "checkbox" => jsonObject.ToObject<CheckboxModel>(),
                "label" => jsonObject.ToObject<LabelModel>(),
                _ => throw new NotSupportedException($"Control type '{type}' is not supported."),
            };

            if (string.IsNullOrEmpty(control.Name))
                control.Name = reader.Path.Split('.').Last();

            return control;
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }
    }
}
