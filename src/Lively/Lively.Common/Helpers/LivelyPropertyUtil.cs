using Lively.Common.JsonConverters;
using Lively.Models.LivelyControls;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
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
                    ScalerDropdownModel scalerDropdown => scalerDropdown.Value,
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

        public static void LocalizeControls(string locPath, IDictionary<string, ControlModel> controls, string languageCode = "")
        {
            if (!File.Exists(locPath))
                return;

            LocalizationFile loc;
            try
            {
                loc = JsonConvert.DeserializeObject<LocalizationFile>(File.ReadAllText(locPath));
            }
            catch {
                return;
            }

            if (loc?.Languages is null)
                return;

            // ApplicationLanguages.PrimaryLanguageOverride is empty when not set / use system default.
            languageCode = string.IsNullOrEmpty(languageCode) ? CultureInfo.CurrentUICulture.Name : languageCode;
            // Try exact match first, eg: zh-CN
            if (!loc.Languages.TryGetValue(languageCode, out var lang))
            {
                // Try base language fallback, eg: zh
                var baseLang = languageCode.Split('-')[0];
                if (!loc.Languages.TryGetValue(baseLang, out lang))
                    return;
            }

            // This is faster than iterating over all controls when some controls are not localized.
            foreach (var localized in lang)
            {
                if (!controls.TryGetValue(localized.Key, out var control))
                    continue;

                var value = localized.Value;
                switch (control.Type)
                {
                    case "dropdown":
                    case "scalerDropdown":
                        {
                            if (control is IDropdownItem dropdown && value.Items != null)
                            {
                                var count = Math.Min(dropdown.Items.Length, value.Items.Length);
                                for (int i = 0; i < count; i++)
                                    dropdown.Items[i] = value.Items[i];
                            }
                        }
                        break;
                    case "label":
                        {
                            if (!string.IsNullOrWhiteSpace(value.Value))
                                ((LabelModel)control).Value = value.Value;
                        }
                        break;
                    case "button":
                        {
                            if (!string.IsNullOrWhiteSpace(value.Value))
                                ((ButtonModel)control).Value = value.Value;
                        }
                        break;
                }
                control.Text = string.IsNullOrWhiteSpace(value.Text) ? control.Text : value.Text;
                control.Help = string.IsNullOrWhiteSpace(value.Help) ? control.Help : value.Help;
            }
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
