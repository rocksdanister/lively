﻿using Lively.Common;
using Lively.Common.Helpers.Files;
using Lively.Models;
using Lively.UI.WinUI.ViewModels;
using Lively.UI.WinUI.Views.Pages;
using Lively.UI.WinUI.Views.Pages.ControlPanel;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel.Resources;
using WinUICommunity;
using static Lively.UI.WinUI.Services.IDialogService;

namespace Lively.UI.WinUI.Services
{
    public class DialogService : IDialogService
    {
        private readonly ResourceLoader i18n;

        public DialogService()
        {
            i18n = ResourceLoader.GetForViewIndependentUse();
        }

        public async Task<DisplayMonitor> ShowDisplayChooseDialogAsync()
        {
            var vm = App.Services.GetRequiredService<ChooseDisplayViewModel>();
            var dialog = new ContentDialog()
            {
                Title = i18n.GetString("DescriptionScreenLayout"),
                Content = new ChooseDisplayView(vm),
                PrimaryButtonText = i18n.GetString("Cancel/Content"),
                //DefaultButton = ContentDialogButton.Primary,
                XamlRoot = App.Services.GetRequiredService<MainWindow>().Content.XamlRoot,
            };
            vm.OnRequestClose += (_, _) => dialog.Hide();
            await dialog.ShowAsyncQueue();
            vm.OnWindowClosing(this, new RoutedEventArgs());
            return vm.SelectedItem?.Screen;
        }

        public async Task<ApplicationModel> ShowApplicationPickerDialogAsync()
        {
            var vm = App.Services.GetRequiredService<FindMoreAppsViewModel>();
            var result = await ShowDialogAsync(new Views.Pages.Settings.FindMoreAppsView() { DataContext = vm },
                                          i18n.GetString("TitleChooseApplication/Text"),
                                          i18n.GetString("TextAdd"),
                                          i18n.GetString("Cancel/Content"));
            return result == DialogResult.primary ? vm.SelectedItem : null;
        }

        public async Task ShowDialogAsync(string message, string title, string primaryBtnText)
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

        public async Task<DialogResult> ShowDialogAsync(object content,
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

        public async Task<string> ShowTextInputDialogAsync(string title)
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

        public async Task ShowThemeDialogAsync()
        {
            await new ContentDialog()
            {
                Title = i18n.GetString("AppTheme/Header"),
                Content = new AppThemeView(),
                PrimaryButtonText = i18n.GetString("TextOk"),
                DefaultButton = ContentDialogButton.Primary,
                XamlRoot = App.Services.GetRequiredService<MainWindow>().Content.XamlRoot,
            }.ShowAsyncQueue();
        }

        public async Task<LibraryModel> ShowDepthWallpaperDialogAsync(string imagePath)
        {
            var vm = App.Services.GetRequiredService<DepthEstimateWallpaperViewModel>();
            vm.SelectedImage = imagePath;
            var depthDialog = new ContentDialog
            {
                Title = i18n.GetString("TitleDepthWallpaper/Content"),
                Content = new DepthEstimateWallpaperView(vm),
                PrimaryButtonText = i18n.GetString("TextContinue/Content"),
                SecondaryButtonText = i18n.GetString("Cancel/Content"),
                DefaultButton = ContentDialogButton.Primary,
                XamlRoot = App.Services.GetRequiredService<MainWindow>().Content.XamlRoot,
                SecondaryButtonCommand = vm.CancelCommand,
                PrimaryButtonCommand = vm.RunCommand,
                IsPrimaryButtonEnabled = vm.IsModelExists,
            };
            vm.OnRequestClose += (_, _) => depthDialog.Hide();
            depthDialog.Closing += (s, e) =>
            {
                if (e.Result == ContentDialogResult.Primary)
                    e.Cancel = true;
            };
            //binding canExecute not working
            vm.RunCommand.CanExecuteChanged += (_, _) =>
            {
                depthDialog.IsPrimaryButtonEnabled = !vm.IsRunning;
            };
            vm.CancelCommand.CanExecuteChanged += (s, e) =>
            {
                depthDialog.IsSecondaryButtonEnabled = !vm.IsRunning;
            };
            await depthDialog.ShowAsyncQueue();
            return vm.NewWallpaper;
        }

        public async Task<WallpaperCreateType?> ShowWallpaperCreateDialogAsync(string filePath)
        {
            if (filePath is null)
                return await InnerShowWallpaperCreateDialog(null);

            //For now only pictures..
            var filter = FileFilter.GetLivelyFileType(filePath);
            if (filter != WallpaperType.picture)
                return WallpaperCreateType.none;

            return await InnerShowWallpaperCreateDialog(filter);
        }

        public async Task<WallpaperCreateType?> ShowWallpaperCreateDialogAsync()
        {
            return await InnerShowWallpaperCreateDialog(null);
        }

        private async Task<WallpaperCreateType?> InnerShowWallpaperCreateDialog(WallpaperType? filter)
        {
            var vm = App.Services.GetRequiredService<AddWallpaperCreateViewModel>();
            var dlg = new ContentDialog()
            {
                Title = i18n.GetString("TitleCreateWallpaper/Content"),
                Content = new AddWallpaperCreateView(vm),
                SecondaryButtonText = i18n.GetString("Cancel/Content"),
                XamlRoot = App.Services.GetRequiredService<MainWindow>().Content.XamlRoot,
            };
            vm.WallpaperCategoriesFiltered.Filter = _ => true; //reset
            if (filter is not null)
                vm.WallpaperCategoriesFiltered.Filter = x => ((AddWallpaperCreateModel)x).TypeSupported == filter;
            else
                vm.WallpaperCategoriesFiltered.Filter = x => ((AddWallpaperCreateModel)x).CreateType != WallpaperCreateType.none;
            vm.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == "SelectedItem")
                    dlg.Hide();
            };
            return await dlg.ShowAsyncQueue()  != ContentDialogResult.Secondary ? vm.SelectedItem.CreateType : null;
        }

        public async Task ShowAboutDialogAsync()
        {
            var vm = App.Services.GetRequiredService<AboutViewModel>();
            await new ContentDialog()
            {
                Title = i18n.GetString("About/Label"),
                Content = new AboutView(vm),
                PrimaryButtonText = i18n.GetString("TextOK"),
                DefaultButton = ContentDialogButton.Primary,
                XamlRoot = App.Services.GetRequiredService<MainWindow>().Content.XamlRoot,
            }.ShowAsyncQueue();
            vm.OnWindowClosing(this, new RoutedEventArgs());
        }

        public async Task ShowControlPanelDialogAsync()
        {
            var vm = App.Services.GetRequiredService<ControlPanelViewModel>();
            await new ContentDialog()
            {
                Title = i18n.GetString("DescriptionScreenLayout"),
                Content = new ControlPanelView(vm),
                PrimaryButtonText = i18n.GetString("TextOK"),
                DefaultButton = ContentDialogButton.Primary,
                XamlRoot = App.Services.GetRequiredService<MainWindow>().Content.XamlRoot,
            }.ShowAsyncQueue();
            vm.OnWindowClosing(this, new RoutedEventArgs());
        }

        public async Task ShowHelpDialogAsync()
        {
            await new ContentDialog()
            {
                Title = i18n.GetString("Help/Label"),
                Content = new HelpView(),
                PrimaryButtonText = i18n.GetString("TextOK"),
                DefaultButton = ContentDialogButton.Primary,
                XamlRoot = App.Services.GetRequiredService<MainWindow>().Content.XamlRoot,
            }.ShowAsyncQueue();
        }
    }
}
