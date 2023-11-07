using Lively.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Lively.Common;
using Microsoft.UI.Xaml;
using Lively.Common.Helpers.Storage;
using System.IO;
using Microsoft.Extensions.DependencyInjection;
using Lively.UI.WinUI.Helpers;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Lively.UI.WinUI.ViewModels
{
    public partial class AddWallpaperDataViewModel : ObservableObject
    {
        private readonly LibraryModel libData;

        public AddWallpaperDataViewModel(LibraryModel obj)
        {
            this.libData = obj;

            //use existing data for editing already imported wallpaper..
            Title = libData.LivelyInfo.Title;
            Desc = libData.LivelyInfo.Desc;
            Url = libData.LivelyInfo.Contact;
            Author = libData.LivelyInfo.Author;
        }

        private string _title;
        public string Title
        {
            get => _title;
            set
            {
                value = (value?.Length > 100 ? value.Substring(0, 100) : value);
                libData.Title = value;
                libData.LivelyInfo.Title = value;
                SetProperty(ref _title, value);
            }
        }

        private string _desc;
        public string Desc
        {
            get => _desc;
            set
            {
                value = (value?.Length > 5000 ? value.Substring(0, 5000) : value);
                libData.Desc = value;
                libData.LivelyInfo.Desc = value;
                SetProperty(ref _desc, value);
            }
        }

        private string _author;
        public string Author
        {
            get => _author;
            set
            {
                value = (value?.Length > 100 ? value.Substring(0, 100) : value);
                libData.Author = value;
                libData.LivelyInfo.Author = value;
                SetProperty(ref _author, value);
            }
        }

        private string _url;
        public string Url
        {
            get => _url;
            set
            {
                libData.LivelyInfo.Contact = value;
                SetProperty(ref _url, value);
            }
        }

        [ObservableProperty]
        private bool isUserEditable = true;

        [ObservableProperty]
        private double currentProgress;

        private RelayCommand _cancelCommand;
        public RelayCommand CancelCommand => _cancelCommand ??=
            new RelayCommand(async () => await OperationCancelled());

        private RelayCommand _proceedCommand;
        public RelayCommand ProceedCommand => _proceedCommand ??=
            new RelayCommand(() => OperationProceed());

        private async Task OperationCancelled()
        {
            var libraryUtil = App.Services.GetRequiredService<LibraryViewModel>();
            await libraryUtil.WallpaperDelete(libData);
        }

        private void OperationProceed()
        {
            JsonStorage<LivelyInfoModel>.StoreData(Path.Combine(libData.LivelyInfoFolderPath, "LivelyInfo.json"), libData.LivelyInfo);
            var libraryUtil = App.Services.GetRequiredService<LibraryViewModel>();
            //libraryUtil.SortWallpaper((LibraryModel)libData);
        }
    }
}
