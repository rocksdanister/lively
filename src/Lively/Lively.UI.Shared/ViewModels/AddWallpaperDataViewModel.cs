using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Lively.Common.Helpers.Storage;
using Lively.Models;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.IO;
using System.Threading.Tasks;

namespace Lively.UI.Shared.ViewModels
{
    public partial class AddWallpaperDataViewModel : ObservableObject
    {
        private readonly LibraryViewModel libraryVm;

        public AddWallpaperDataViewModel(LibraryViewModel libraryVm)
        {
            this.libraryVm = libraryVm;
        }

        private LibraryModel _model;
        public LibraryModel Model
        {
            get => _model;
            set
            {
                //use existing data for editing already imported wallpaper..
                Title = value?.LivelyInfo.Title;
                Desc = value?.LivelyInfo.Desc;
                Url = value?.LivelyInfo.Contact;
                Author = value?.LivelyInfo.Author;
                _model = value;
            }
        }

        private string _title;
        public string Title
        {
            get => _title;
            set
            {
                value = (value?.Length > 100 ? value.Substring(0, 100) : value);
                Model.Title = value;
                Model.LivelyInfo.Title = value;
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
                Model.Desc = value;
                Model.LivelyInfo.Desc = value;
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
                Model.Author = value;
                Model.LivelyInfo.Author = value;
                SetProperty(ref _author, value);
            }
        }

        private string _url;
        public string Url
        {
            get => _url;
            set
            {
                Model.LivelyInfo.Contact = value;
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
            await libraryVm.WallpaperDelete(Model);
        }

        private void OperationProceed()
        {
            JsonStorage<LivelyInfoModel>.StoreData(Path.Combine(Model.LivelyInfoFolderPath, "LivelyInfo.json"), Model.LivelyInfo);
            //libraryVm.SortWallpaper(Model);
        }
    }
}
