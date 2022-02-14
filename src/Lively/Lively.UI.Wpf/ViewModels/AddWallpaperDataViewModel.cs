using Lively.Common;
using Lively.Common.Helpers.MVVM;
using Lively.Common.Helpers.Storage;
using Lively.Models;
using Lively.UI.Wpf.Helpers;
using Lively.UI.Wpf.Helpers.MVVM;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Lively.UI.Wpf.ViewModels
{
    public class AddWallpaperDataViewModel : ObservableObject
    {
        private readonly ILibraryModel libData;

        public AddWallpaperDataViewModel(ILibraryModel obj)
        {
            this.libData = obj;

            //use existing data for editing already imported wallpaper..
            Title = libData.LivelyInfo.Title;
            Desc = libData.LivelyInfo.Desc;
            Url = libData.LivelyInfo.Contact;
            Author = libData.LivelyInfo.Author;
        }

        #region data

        private string _title;
        public string Title
        {
            get { return _title; }
            set
            {
                _title = (value?.Length > 100 ? value.Substring(0, 100) : value);
                libData.Title = _title;
                libData.LivelyInfo.Title = _title;
                OnPropertyChanged();
            }
        }

        private string _desc;
        public string Desc
        {
            get { return _desc; }
            set
            {
                _desc = (value?.Length > 5000 ? value.Substring(0, 5000) : value);
                libData.Desc = _desc;
                libData.LivelyInfo.Desc = _desc;
                OnPropertyChanged();
            }
        }

        private string _author;
        public string Author
        {
            get { return _author; }
            set
            {
                _author = (value?.Length > 100 ? value.Substring(0, 100) : value);
                libData.Author = _author;
                libData.LivelyInfo.Author = _author;
                OnPropertyChanged();
            }
        }

        private string _url;
        public string Url
        {
            get { return _url; }
            set
            {
                _url = value;
                try
                {
                    libData.SrcWebsite = LinkHandler.SanitizeUrl(_url);
                }
                catch
                {
                    libData.SrcWebsite = null;
                }
                libData.LivelyInfo.Contact = _url;
                OnPropertyChanged();
            }
        }

        #endregion //data

        private bool _isUserEditable = true;
        public bool IsUserEditable
        {
            get { return _isUserEditable; }
            set
            {
                _isUserEditable = value;
                OnPropertyChanged();
            }
        }

        private double _currentProgress;
        public double CurrentProgress
        {
            get { return _currentProgress; }
            set
            {
                _currentProgress = value;
                OnPropertyChanged();
            }
        }

        private RelayCommand _cancelCommand;
        public RelayCommand CancelCommand => _cancelCommand ??=
            new RelayCommand(async param => await OperationCancelled());

        private RelayCommand _proceedCommand;
        public RelayCommand ProceedCommand => _proceedCommand ??=
            new RelayCommand(param => OperationProceed());

        private async Task OperationCancelled()
        {
            var libraryUtil = App.Services.GetRequiredService<LibraryUtil>();
            await libraryUtil.WallpaperDelete(libData);
        }

        private void OperationProceed()
        {
            JsonStorage<LivelyInfoModel>.StoreData(Path.Combine(libData.LivelyInfoFolderPath, "LivelyInfo.json"), libData.LivelyInfo);
        }
    }
}
