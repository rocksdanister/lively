using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Lively.Common;
using Lively.Common.Helpers;
using Lively.Grpc.Client;
using Lively.Models;
using Lively.UI.WinUI.Extensions;
using Lively.UI.WinUI.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Dispatching;
using System;
using System.Globalization;
using System.Threading.Tasks;
using Windows.Storage.Pickers;

namespace Lively.UI.WinUI.ViewModels.Settings
{
    public partial class SettingsSystemViewModel : ObservableObject
    {
        private readonly DispatcherQueue dispatcherQueue;

        private readonly IUserSettingsClient userSettings;
        private readonly IDialogService dialogService;
        private readonly ICommandsClient commands;

        public SettingsSystemViewModel(IUserSettingsClient userSettings, 
            ICommandsClient commands,
            IDialogService dialogService)
        {
            this.userSettings = userSettings;
            this.commands = commands;
            this.dialogService = dialogService;

            //MainWindow dispatcher may not be ready yet, creating our own instead..
            dispatcherQueue = DispatcherQueue.GetForCurrentThread() ?? DispatcherQueueController.CreateOnCurrentThread().DispatcherQueue;

            IsDesktopAutoWallpaper = userSettings.Settings.DesktopAutoWallpaper;
            SelectedTaskbarThemeIndex = (int)userSettings.Settings.SystemTaskbarTheme;
            //IsLockScreenAutoWallpaper = userSettings.Settings.LockScreenAutoWallpaper;
        }

        private bool _isDesktopAutoWallpaper;
        public bool IsDesktopAutoWallpaper
        {
            get => _isDesktopAutoWallpaper;
            set
            {
                if (userSettings.Settings.DesktopAutoWallpaper != value)
                {
                    userSettings.Settings.DesktopAutoWallpaper = value;
                    UpdateSettingsConfigFile();
                }
                SetProperty(ref _isDesktopAutoWallpaper, value);
            }
        }

        private int _selectedTaskbarThemeIndex;
        public int SelectedTaskbarThemeIndex
        {
            get => _selectedTaskbarThemeIndex;
            set
            {
                if (userSettings.Settings.SystemTaskbarTheme != (TaskbarTheme)value)
                {
                    userSettings.Settings.SystemTaskbarTheme = (TaskbarTheme)value;
                    UpdateSettingsConfigFile();
                }
                SetProperty(ref _selectedTaskbarThemeIndex, value);
            }
        }

        [RelayCommand]
        private void ShowDebug()
        {
            commands.ShowDebugger();
        }

        [RelayCommand]
        private async Task ExtractLog()
        {
            var filePicker = new FileSavePicker();
            filePicker.SetOwnerWindow(App.Services.GetRequiredService<MainWindow>());
            filePicker.FileTypeChoices.Add("Compressed archive", [".zip"]);
            filePicker.SuggestedFileName = "lively_log_" + DateTime.Now.ToString("yyyyMMdd_HHmmss", CultureInfo.InvariantCulture);
            var file = await filePicker.PickSaveFileAsync();
            if (file != null)
            {
                try
                {
                    LogUtil.ExtractLogFiles(file.Path);
                }
                catch (Exception ex)
                {
                    await dialogService.ShowDialogAsync(ex.Message, "Error", "OK");
                }
            }
        }

        public void UpdateSettingsConfigFile()
        {
            _ = dispatcherQueue.TryEnqueue(() =>
            {
                userSettings.Save<SettingsModel>();
            });
        }
    }
}
