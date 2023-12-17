﻿using CommunityToolkit.Mvvm.ComponentModel;
using Lively.Grpc.Client;
using Lively.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lively.UI.WinUI.ViewModels
{
    public partial class ChooseDisplayViewModel : ObservableObject
    {
        public event EventHandler OnRequestClose;

        private readonly IDisplayManagerClient displayManager;
        private readonly IDesktopCoreClient desktopCore;

        public ChooseDisplayViewModel(IDesktopCoreClient desktopCore, IDisplayManagerClient displayManager)
        {
            this.desktopCore = desktopCore;
            this.displayManager = displayManager;
            desktopCore.WallpaperChanged += SetupDesktop_WallpaperChanged;

            ScreenItems = new ObservableCollection<ScreenLayoutModel>();
            UpdateLayout();
        }

        private void SetupDesktop_WallpaperChanged(object sender, EventArgs e)
        {
            _ = App.Services.GetRequiredService<MainWindow>().DispatcherQueue.TryEnqueue(() =>
            {
                UpdateLayout();
            });
        }

        [ObservableProperty]
        private ObservableCollection<ScreenLayoutModel> screenItems;

        private ScreenLayoutModel _selectedItem;
        public ScreenLayoutModel SelectedItem
        {
            get => _selectedItem;
            set
            {
                SetProperty(ref _selectedItem, value);
                OnRequestClose?.Invoke(this, EventArgs.Empty);
            }
        }

        public void OnWindowClosing(object sender, RoutedEventArgs e)
            => desktopCore.WallpaperChanged -= SetupDesktop_WallpaperChanged;

        private void UpdateLayout()
        {
            ScreenItems.Clear();
            var unsortedScreenItems = new List<ScreenLayoutModel>();
            foreach (var item in displayManager.DisplayMonitors)
            {
                string imgPath = null;
                string livelyPropertyFilePath = null;
                foreach (var x in desktopCore.Wallpapers)
                {
                    if (item.Equals(x.Display))
                    {
                        imgPath = string.IsNullOrEmpty(x.PreviewPath) ? x.ThumbnailPath : x.PreviewPath;
                        livelyPropertyFilePath = x.LivelyPropertyCopyPath;
                    }
                }
                unsortedScreenItems.Add(
                    new ScreenLayoutModel(item, imgPath, livelyPropertyFilePath, item.Index.ToString()));
            }

            foreach (var item in unsortedScreenItems.OrderBy(x => x.Screen.Bounds.X).ToList())
            {
                ScreenItems.Add(item);
            }
        }
    }
}
