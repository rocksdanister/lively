using Lively.Common;
using Lively.Common.Services;
using Lively.Models;
using Lively.Models.Enums;
using Lively.Models.Gallery.API;
using Lively.UI.Shared.ViewModels;
using Lively.UI.WinUI.Extensions;
using Lively.UI.WinUI.Views.LivelyProperty;
using Lively.UI.WinUI.Views.Pages;
using Lively.UI.WinUI.Views.Pages.ControlPanel;
using Lively.UI.WinUI.Views.Pages.Gallery;
using Lively.UI.WinUI.Views.Pages.Settings;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Lively.UI.WinUI.Services
{
    public class DialogService : IDialogService
    {
        public bool IsWorking { get; private set; }

        private readonly IResourceService i18n;

        public DialogService(IResourceService i18n)
        {
            this.i18n = i18n;
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
            var result = await ShowDialogAsync(new FindMoreAppsView() { DataContext = vm },
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
                Content = new ScrollViewer()
                {
                    Content = new TextBlock()
                    {
                        Text = message,
                        TextWrapping = TextWrapping.Wrap
                    } 
                },
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

        public async Task<string> ShowTextInputDialogAsync(string title, string placeholderText)
        {
            var tb = new TextBox()
            {
                Height = 75,
                Padding = new Thickness(10),
                TextWrapping = TextWrapping.Wrap,
                PlaceholderText = placeholderText
            };
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

        public async Task ShowCustomiseWallpaperDialogAsync(LibraryModel obj)
        {
            try
            {
                IsWorking = true;

                var vm = App.Services.GetRequiredService<CustomiseWallpaperViewModel>();
                var dialog = new ContentDialog()
                {
                    Title = obj.Title.Length > 35 ? obj.Title.Substring(0, 35) + "..." : obj.Title,
                    Content = new LivelyPropertiesView(vm) { MinWidth = 325 },
                    PrimaryButtonText = i18n.GetString("TextOk"),
                    DefaultButton = ContentDialogButton.Primary,
                    XamlRoot = App.Services.GetRequiredService<MainWindow>().Content.XamlRoot,
                };
                dialog.Closing += (s, e) =>
                {
                    vm.OnClose();
                };
                vm.Load(obj);
                await dialog.ShowAsyncQueue();
            }
            finally
            {
                IsWorking = false;
            }
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

        public async Task<(WallpaperAddType wallpaperType, List<string> wallpapers)> ShowAddWallpaperDialogAsync()
        {
            (WallpaperAddType, List<string>) result = (WallpaperAddType.none, null);
            var addVm = App.Services.GetRequiredService<AddWallpaperViewModel>();
            var addDialog = new ContentDialog()
            {
                Title = i18n.GetString("AddWallpaper/Label"),
                Content = new AddWallpaperView(addVm),
                PrimaryButtonText = i18n.GetString("TextOK"),
                DefaultButton = ContentDialogButton.Primary,
                XamlRoot = App.Services.GetRequiredService<MainWindow>().Content.XamlRoot,
            };

            addVm.OnRequestAddUrl += (_, e) =>
            {
                result = (WallpaperAddType.url, new List<string>() { e });
                addDialog.Hide();
            };
            addVm.OnRequestAddFile += (_, e) =>
            {
                result = (WallpaperAddType.files, e);
                addDialog.Hide();
            };
            addVm.OnRequestOpenCreate += (_, _) =>
            {
                result = (WallpaperAddType.create, null);
                addDialog.Hide();
            };
            await addDialog.ShowAsyncQueue();
            return result;
        }

        public async Task<WallpaperCreateType?> ShowWallpaperCreateDialogAsync(string filePath)
        {
            if (filePath is null)
                return await InnerShowWallpaperCreateDialog(null);

            //For now only pictures..
            var filter = FileTypes.GetFileType(filePath);
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
            await new ContentDialog()
            {
                Title = i18n.GetString("About/Label"),
                Content = new AboutView(),
                PrimaryButtonText = i18n.GetString("TextOK"),
                DefaultButton = ContentDialogButton.Primary,
                XamlRoot = App.Services.GetRequiredService<MainWindow>().Content.XamlRoot,
            }.ShowAsyncQueue();
        }

        public async Task ShowPatreonSupportersDialogAsync()
        {
            var page = new PatreonSupportersView();
            var dlg = new ContentDialog()
            {
                Title = i18n.GetString("TitlePatreon/Text"),
                Content = page,
                PrimaryButtonText = i18n.GetString("TextBecomePatreonMember/Content"),
                SecondaryButtonText = i18n.GetString("Cancel/Content"),
                DefaultButton = ContentDialogButton.Primary,
                XamlRoot = App.Services.GetRequiredService<MainWindow>().Content.XamlRoot,
            };
            dlg.Resources["ContentDialogMinWidth"] = 640;

            if (await dlg.ShowAsyncQueue() == ContentDialogResult.Primary)
                LinkUtil.OpenBrowser("https://rocksdanister.github.io/lively/coffee/");

            page.OnClose();
        }

        public async Task ShowControlPanelDialogAsync()
        {
            var isDialogVisible = true;
            var vm = App.Services.GetRequiredService<ControlPanelViewModel>();
            var dialog = new ContentDialog()
            {
                Title = i18n.GetString("DescriptionScreenLayout"),
                Content = new ControlPanelView(vm),
                PrimaryButtonText = i18n.GetString("TextOK"),
                DefaultButton = ContentDialogButton.Primary,
                XamlRoot = App.Services.GetRequiredService<MainWindow>().Content.XamlRoot,
            };
            dialog.Closed += OnDialogClose;
            vm.PropertyChanged += PropertyChanged;
            await dialog.ShowAsyncQueue();

            async void PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
            {
                if (e.PropertyName == nameof(vm.IsHideDialog))
                {
                    if (vm.IsHideDialog)
                    {
                        isDialogVisible = false;
                        dialog.Hide();
                    }
                    else
                    {
                        isDialogVisible = true;
                        // Re-open the dialog
                        await dialog.ShowAsyncQueue();
                    }
                }
            }

            void OnDialogClose(object sender, ContentDialogClosedEventArgs args)
            {
                if (isDialogVisible)
                    OnWindowClose();
            }

            void OnWindowClose()
            {
                vm.OnWindowClosing(this, EventArgs.Empty);
                vm.PropertyChanged -= PropertyChanged;
                dialog.Closed -= OnDialogClose;
            }
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

        public async Task ShowShareWallpaperDialogAsync(LibraryModel obj)
        {
            var vm = App.Services.GetRequiredService<ShareWallpaperViewModel>();
            vm.Model = obj;
            await new ContentDialog()
            {
                Title = i18n.GetString("TitleShareWallpaper/Text"),
                Content = new ShareWallpaperView()
                {
                    DataContext = vm,
                },
                PrimaryButtonText = i18n.GetString("TextOK"),
                DefaultButton = ContentDialogButton.Primary,
                XamlRoot = App.Services.GetRequiredService<MainWindow>().Content.XamlRoot,
            }.ShowAsyncQueue();
        }

        public async Task ShowAboutWallpaperDialogAsync(LibraryModel obj)
        {
            await new ContentDialog()
            {
                Title = i18n.GetString("About/Label"),
                Content = new LibraryAboutView()
                {
                    DataContext = new LibraryAboutViewModel(obj),
                },
                PrimaryButtonText = i18n.GetString("TextOK"),
                DefaultButton = ContentDialogButton.Primary,
                XamlRoot = App.Services.GetRequiredService<MainWindow>().Content.XamlRoot,
            }.ShowAsyncQueue();
        }

        public async Task<bool> ShowDeleteWallpaperDialogAsync(LibraryModel obj)
        {
            return await new ContentDialog()
            {
                Title = obj.LivelyInfo.IsAbsolutePath ?
                i18n.GetString("DescriptionDeleteConfirmationLibrary") : i18n.GetString("DescriptionDeleteConfirmation"),
                Content = new LibraryAboutView() { DataContext = new LibraryAboutViewModel(obj) },
                PrimaryButtonText = i18n.GetString("TextYes"),
                SecondaryButtonText = i18n.GetString("TextNo"),
                DefaultButton = ContentDialogButton.Primary,
                XamlRoot = App.Services.GetRequiredService<MainWindow>().Content.XamlRoot,
            }.ShowAsyncQueue() == ContentDialogResult.Primary;
        }

        public async Task ShowReportWallpaperDialogAsync(LibraryModel obj)
        {
            await new ContentDialog()
            {
                Title = i18n.GetString("TitleReportWallpaper/Text"),
                Content = new ReportWallpaperView()
                {
                    DataContext = new ReportWallpaperViewModel(obj),
                },
                PrimaryButtonText = i18n.GetString("Send/Content"),
                SecondaryButtonText = i18n.GetString("Cancel/Content"),
                DefaultButton = ContentDialogButton.Primary,
                XamlRoot = App.Services.GetRequiredService<MainWindow>().Content.XamlRoot,
            }.ShowAsyncQueue();
        }

        public async Task<IEnumerable<GalleryModel>> ShowGalleryRestoreWallpaperDialogAsync(IEnumerable<WallpaperDto> wallpapers)
        {
            if (!wallpapers.Any())
                return null;

            var vm = App.Services.GetRequiredService<RestoreWallpaperViewModel>();
            foreach (var item in wallpapers)
                vm.Wallpapers.Add(new GalleryModel(item, false) { IsSelected = true });

            var result = await ShowDialogAsync(
                new RestoreWallpaperView(vm),
                i18n.GetString("TitleWelcomeback/Text"),
                i18n.GetString("TextDownloadNow/Content"),
                i18n.GetString("TextMaybeLater/Content"));

            return result == DialogResult.primary ? vm.SelectedItems : null;
        }

        public async Task ShowGalleryEditProfileDialogAsync()
        {
            await new ContentDialog()
            {
                Title = "Account",
                Content = new ManageAccountView(),
                PrimaryButtonText = i18n.GetString("TextOK"),
                DefaultButton = ContentDialogButton.Primary,
                XamlRoot = App.Services.GetRequiredService<MainWindow>().Content.XamlRoot,
            }.ShowAsyncQueue();
        }

        public async Task ShowWaitDialogAsync(object content, int seconds)
        {
            var dlg = new ContentDialog()
            {
                Title = i18n.GetString("PleaseWait/Text"),
                Content = content,
                PrimaryButtonText = $"{seconds}s",
                IsPrimaryButtonEnabled = false,
                DefaultButton = ContentDialogButton.Primary,
                XamlRoot = App.Services.GetRequiredService<MainWindow>().Content.XamlRoot,
            };
            dlg.Opened += async (s, e) =>
            {
                for (int i = seconds; i > 0; i--)
                {
                    dlg.PrimaryButtonText = $"{i}s";
                    await Task.Delay(1000);
                }
                dlg.PrimaryButtonText = i18n.GetString("TextOK");
                dlg.IsPrimaryButtonEnabled = true;
            };
            await dlg.ShowAsyncQueue();
        }
    }
}
