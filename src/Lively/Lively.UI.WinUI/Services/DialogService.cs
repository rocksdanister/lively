using Lively.Common;
using Lively.Common.Helpers.Files;
using Lively.Models;
using Lively.UI.WinUI.ViewModels;
using Lively.UI.WinUI.Views.Pages;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel.Resources;
using WinUICommunity;
using static Lively.UI.WinUI.Services.IDialogService;
using static WinUICommunity.LanguageDictionary;

namespace Lively.UI.WinUI.Services
{
    public class DialogService : IDialogService
    {
        private readonly ResourceLoader i18n;

        public DialogService()
        {
            i18n = ResourceLoader.GetForViewIndependentUse();
        }

        public async Task<IDisplayMonitor> ShowDisplayChooseDialog()
        {
            var vm = App.Services.GetRequiredService<ChooseDisplayViewModel>();
            var dialog = new ContentDialog()
            {
                Title = i18n.GetString("DescriptionScreenLayout"),
                Content = new ChooseDisplayView(vm),
                PrimaryButtonText = i18n.GetString("TextClose"),
                DefaultButton = ContentDialogButton.Primary,
                XamlRoot = App.Services.GetRequiredService<MainWindow>().Content.XamlRoot,
            };
            vm.OnRequestClose += (_, _) => dialog.Hide();
            await dialog.ShowAsyncQueue();

            return vm.SelectedItem?.Screen;
        }

        public async Task<ApplicationModel> ShowApplicationPickerDialog()
        {
            var vm = App.Services.GetRequiredService<FindMoreAppsViewModel>();
            var result = await ShowDialog(new Views.Pages.Settings.FindMoreAppsView() { DataContext = vm },
                                          i18n.GetString("TitleChooseApplication/Text"),
                                          i18n.GetString("TextAdd"),
                                          i18n.GetString("Cancel/Content"));
            return result == DialogResult.primary ? vm.SelectedItem : null;
        }

        public async Task ShowDialog(string message, string title, string primaryBtnText)
        {
            await new ContentDialog()
            {
                Title = title,
                Content = new TextBlock() { Text = message },
                PrimaryButtonText = primaryBtnText,
                DefaultButton = ContentDialogButton.Primary,
                XamlRoot = App.Services.GetRequiredService<MainWindow>().Content.XamlRoot,
            }.ShowAsyncQueue();
        }

        public async Task<DialogResult> ShowDialog(object content,
            string title,
            string primaryBtnText,
            string secondaryBtnText,
            bool isDefaultPrimary = true)
        {
            var result = await new ContentDialog()
            {
                Title = title,
                Content = content,
                PrimaryButtonText = primaryBtnText,
                SecondaryButtonText = secondaryBtnText,
                DefaultButton = isDefaultPrimary ? ContentDialogButton.Primary : ContentDialogButton.Secondary,
                XamlRoot = App.Services.GetRequiredService<MainWindow>().Content.XamlRoot,
            }.ShowAsyncQueue();

            return result switch
            {
                ContentDialogResult.None => DialogResult.none,
                ContentDialogResult.Primary => DialogResult.primary,
                ContentDialogResult.Secondary => DialogResult.seconday,
                _ => DialogResult.none,
            };
        }

        public async Task<string> ShowTextInputDialog(string title)
        {
            var tb = new TextBox();
            var dialog = new ContentDialog
            {
                Title = title,
                Content = tb,
                PrimaryButtonText = i18n.GetString("TextOK"),
                XamlRoot = App.Services.GetRequiredService<MainWindow>().Content.XamlRoot,
            };
            await dialog.ShowAsyncQueue();
            return tb.Text;
        }

        public async Task ShowThemeDialog()
        {
            await new ContentDialog()
            {
                Title = i18n.GetString("AppTheme/Header"),
                Content = new ThemeView(),
                PrimaryButtonText = i18n.GetString("TextOk"),
                DefaultButton = ContentDialogButton.Primary,
                XamlRoot = App.Services.GetRequiredService<MainWindow>().Content.XamlRoot,
            }.ShowAsyncQueue();
        }

        public async Task<ILibraryModel> ShowDepthWallpaperDialog(string imagePath)
        {
            var vm = App.Services.GetRequiredService<DepthEstimateWallpaperViewModel>();
            vm.SelectedImage = imagePath;
            var depthDialog = new ContentDialog
            {
                Title = "AI Depth Wallpaper",
                Content = new DepthEstimateWallpaperView(vm),
                PrimaryButtonText = "Continue",
                SecondaryButtonText = "Cancel",
                DefaultButton = ContentDialogButton.Primary,
                XamlRoot = App.Services.GetRequiredService<MainWindow>().Content.XamlRoot,
                PrimaryButtonCommand = vm.RunCommand
            };
            vm.OnRequestClose += (_, _) => depthDialog.Hide();
            depthDialog.Closing += (s, e) =>
            {
                if (e.Result == ContentDialogResult.Primary)
                {
                    e.Cancel = true;
                }
            };
            vm.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == "IsRunning")
                {
                    depthDialog.IsPrimaryButtonEnabled = !vm.IsRunning;
                    depthDialog.IsSecondaryButtonEnabled = !vm.IsRunning;
                }
            };
            await depthDialog.ShowAsyncQueue();
            return vm.NewWallpaper;
        }

        public async Task<WallpaperCreateType?> ShowWallpaperCreateDialog(WallpaperType? filter)
        {
            //For now only pictures..
            if (filter != WallpaperType.picture)
                return WallpaperCreateType.none;

            var vm = App.Services.GetRequiredService<AddWallpaperCreateViewModel>();
            var dlg = new ContentDialog()
            {
                Title = "Create wallpaper",
                Content = new AddWallpaperCreateView(vm),
                PrimaryButtonText = "Continue",
                SecondaryButtonText = "Cancel",
                DefaultButton = ContentDialogButton.Primary,
                XamlRoot = App.Services.GetRequiredService<MainWindow>().Content.XamlRoot,
            };
            vm.Filter(filter);
            var dlgResult = await dlg.ShowAsyncQueue();
            return  dlgResult == ContentDialogResult.Primary ? vm.SelectedItem.CreateType : null;
        }
    }
}
