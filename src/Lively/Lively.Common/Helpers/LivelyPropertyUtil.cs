using Lively.Common.JsonConverters;
using Lively.Models.LivelyControls;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace Lively.Common.Helpers
{
    public static class LivelyPropertyUtil
    {
        public delegate Task ExecuteScriptDelegate(string key, object value);

        public static async Task LoadProperty(string propertyPath, string rootDir, ExecuteScriptDelegate execute)
        {
            if (!File.Exists(propertyPath))
                return;

            var controls = GetControls(propertyPath);
            foreach (var control in controls.Values) 
            {
                // Skip, user interaction only.
                if (control is ButtonModel || control is LabelModel)
                    continue;

                object value = control switch
                {
                    SliderModel slider => slider.Value,
                    DropdownModel dropdown => dropdown.Value,
                    FolderDropdownModel folderDropdown => GetFolderDropdownValue(folderDropdown, rootDir),
                    CheckboxModel checkbox => checkbox.Value,
                    TextboxModel textbox => textbox.Value,
                    ColorPickerModel colorPicker => colorPicker.Value,
                    _ => throw new NotSupportedException($"Unsupported control type: {control.Type}")
                };

                await execute(control.Name, value);
            }
        }

        public static void LoadProperty(string propertyPath, Action<ControlModel> execute)
        {
            if (!File.Exists(propertyPath))
                return;

            var controls = GetControls(propertyPath);
            foreach (var control in controls.Values)
            {
                // Skip, user interaction only.
                if (control is ButtonModel || control is LabelModel)
                    continue;

                execute(control);
            }
        }

        public static Dictionary<string, ControlModel> GetControls(string propertyPath)
        {
            var jsonSerializerSettings = new JsonSerializerSettings { Converters = new List<JsonConverter> { new LivelyControlModelConverter() } };
            return JsonConvert.DeserializeObject<Dictionary<string, ControlModel>>(File.ReadAllText(propertyPath), jsonSerializerSettings);
        }

        private static string GetFolderDropdownValue(FolderDropdownModel fd, string rootPath)
        {
            // It is null when no item is selected or file missing.
            var relativeFilePath = fd.Value is null || fd.Folder is null ? null : Path.Combine(fd.Folder, fd.Value);
            var filePath =  relativeFilePath is null ? null : Path.Combine(rootPath, relativeFilePath);
            return File.Exists(filePath) ? relativeFilePath : null;
        }
    }
}
