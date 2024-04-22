using Lively.Models.LivelyControls;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lively.UI.WinUI.Helpers
{
    public class LivelyControlTemplateSelector : DataTemplateSelector
    {
        public DataTemplate SliderTemplate { get; set; }
        public DataTemplate TextBoxTemplate { get; set; }
        public DataTemplate DropdownTemplate { get; set; }
        public DataTemplate FolderDropdownTemplate { get; set; }
        public DataTemplate ButtonTemplate { get; set; }
        public DataTemplate ColorPickerTemplate { get; set; }
        public DataTemplate CheckboxTemplate { get; set; }
        public DataTemplate LabelTemplate { get; set; }

        protected override DataTemplate SelectTemplateCore(object item)
        {
            if (item is ControlModel control)
            {
                return control.Type.ToLower() switch
                {
                    "slider" => SliderTemplate,
                    "textbox" => TextBoxTemplate,
                    "dropdown" => DropdownTemplate,
                    "folderdropdown" => FolderDropdownTemplate,
                    "scalerdropdown" => DropdownTemplate,
                    "button" => ButtonTemplate,
                    "color" => ColorPickerTemplate,
                    "checkbox" => CheckboxTemplate,
                    "label" => LabelTemplate,
                    _ => throw new NotSupportedException($"Control type '{control.Type}' is not supported."),
                };
            }
            throw new InvalidOperationException();
        }
    }
}
