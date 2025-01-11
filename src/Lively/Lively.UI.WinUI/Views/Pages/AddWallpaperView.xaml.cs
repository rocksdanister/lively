using Lively.UI.Shared.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.IO;
using System.Linq;
using Windows.ApplicationModel.DataTransfer;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Lively.UI.WinUI.Views.Pages
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class AddWallpaperView : Page
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
        private readonly AddWallpaperViewModel vm;

        public AddWallpaperView(AddWallpaperViewModel vm)
        {
            this.vm = vm;

            this.InitializeComponent();
            this.DataContext = vm;
        }

        private async void Page_Drop(object sender, DragEventArgs e)
        {
            this.addPanel.Visibility = Visibility.Visible;
            this.addPanelDrop.Visibility = Visibility.Collapsed;

            if (e.DataView.Contains(StandardDataFormats.WebLink))
            {
                var uri = await e.DataView.GetWebLinkAsync();
                Logger.Info($"Dropped string {uri}");

                vm.AddWallpaperLink(uri);
            }
            else if (e.DataView.Contains(StandardDataFormats.StorageItems))
            {
                var items = await e.DataView.GetStorageItemsAsync();
                if (items.Count == 1)
                {
                    var item = items[0].Path;
                    Logger.Info($"Dropped file {item}");
                    try
                    {
                        if (string.IsNullOrWhiteSpace(Path.GetExtension(item)))
                            return;
                    }
                    catch (ArgumentException)
                    {
                        Logger.Info($"Invalid character, skipping dropped file {item}");
                        return;
                    }

                    vm.AddWallpaperFile(item);
                }
                else if (items.Count > 1)
                {
                    vm.AddWallpaperFiles(items.Select(x => x.Path).ToList());
                }
            }
        }

        private void Page_DragOver(object sender, DragEventArgs e)
        {
            e.AcceptedOperation = DataPackageOperation.Copy;
            this.addPanel.Visibility = Visibility.Collapsed;
            this.addPanelDrop.Visibility = Visibility.Visible;
        }

        private void Page_DragLeave(object sender, DragEventArgs e)
        {
            this.addPanel.Visibility = Visibility.Visible;
            this.addPanelDrop.Visibility = Visibility.Collapsed;
        }
    }
}