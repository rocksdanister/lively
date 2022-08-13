using Lively.Models;
using Lively.UI.WinUI.ViewModels;
using Lively.UI.WinUI.Views.Pages;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml.Controls;
using SettingsUI.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel.Resources;
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
                _=> DialogResult.none,
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
    }
}
