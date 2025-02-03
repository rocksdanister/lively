using Lively.Models.UserControls;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Lively.UI.WinUI.Helpers
{
    public class NavigationMenuItemTemplateSelector : DataTemplateSelector
    {
        public DataTemplate MainTemplate { get; set; }
        public DataTemplate SettingsTemplate { get; set; }

        protected override DataTemplate SelectTemplateCore(object item)
        {
            return item is NavigationItem navItem && !string.IsNullOrEmpty(navItem.Glyph) ? 
                MainTemplate : SettingsTemplate;
        }
    }
}
