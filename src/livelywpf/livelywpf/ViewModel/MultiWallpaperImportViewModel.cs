using livelywpf.Helpers;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using System.Threading.Tasks;
using System.Linq;
using System.ComponentModel;
using System.Threading;

namespace livelywpf 
{
    class MultiWallpaperImportViewModel : ObservableObject
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
        private readonly CancellationTokenSource cancellationTokenSrc = new CancellationTokenSource();
        private LibraryTileType importFlag;
        private readonly int totalItems = 0;
        private bool _isRunning = false;

        public MultiWallpaperImportViewModel(List<string> paths)
        {
            int id = 1;
            //display all Lively zip files first since its the first items to get processed..
            paths = paths.OrderByDescending(x => System.IO.Path.GetExtension(x).Equals(".zip", StringComparison.OrdinalIgnoreCase)).ToList();
            for (int i = 0; i < paths.Count; i++)
            {
                var type = FileFilter.GetLivelyFileType(paths[i]);
                if (type != (WallpaperType)(-1))
                {
                    if (type == (WallpaperType)100)
                    {
                        if (ZipExtract.CheckLivelyZip(paths[i]))
                        {
                            ListItems.Add(new MultiWallpaperImportModel(paths[i], type, id++));
                        }
                        else
                        {
                            Logger.Info("Not Lively .zip format:" + paths[i]);
                        }
                    }
                    else
                    {
                        ListItems.Add(new MultiWallpaperImportModel(paths[i], type, id++));
                    }
                }
                else
                {
                    Logger.Info("Wallpaper format not supported:" + paths[i]);
                }
            }

            GifCheck = Program.SettingsVM.Settings.GifCapture;
            AutoImportCheck = Program.SettingsVM.Settings.MultiFileAutoImport;
            totalItems = ListItems.Count;

            SetupDesktop.WallpaperChanged += SetupDesktop_WallpaperChanged;
        }

        private ObservableCollection<MultiWallpaperImportModel> _listItems = new ObservableCollection<MultiWallpaperImportModel>();
        public ObservableCollection<MultiWallpaperImportModel> ListItems
        {
            get { return _listItems; }
            set
            {
                if (value != _listItems)
                {
                    _listItems = value;
                    OnPropertyChanged("ListItems");
                }
            }
        }

        private MultiWallpaperImportModel _selectedItem;
        public MultiWallpaperImportModel SelectedItem
        {
            get { return _selectedItem; }
            set
            {
                if (value != _selectedItem)
                {
                    _selectedItem = value;
                    OnPropertyChanged("SelectedItem");
                }
            }
        }

        private bool _gifCheck;
        public bool GifCheck
        {
            get { return _gifCheck; }
            set
            {
                _gifCheck = value;
                if (_gifCheck != Program.SettingsVM.Settings.GifCapture)
                {
                    Program.SettingsVM.Settings.GifCapture = _gifCheck;
                    Program.SettingsVM.UpdateConfigFile();
                }
                OnPropertyChanged("GifCheck");
            }
        }

        private bool _autoImportCheck;
        public bool AutoImportCheck
        {
            get { return _autoImportCheck; }
            set
            {
                _autoImportCheck = value;
                if (_autoImportCheck != Program.SettingsVM.Settings.MultiFileAutoImport)
                {
                    Program.SettingsVM.Settings.MultiFileAutoImport = _autoImportCheck;
                    Program.SettingsVM.UpdateConfigFile();
                }
                OnPropertyChanged("AutoImportCheck");
            }
        }


        public double _progress = 0;
        public double Progress
        {
            get { return _progress; }
            set
            {
                if (value != _progress)
                {
                    _progress = value;
                    OnPropertyChanged("Progress");
                }
            }
        }

        private RelayCommand _btnCommand;
        public RelayCommand BtnCommand
        {
            get
            {
                if (_btnCommand == null)
                {
                    _btnCommand = new RelayCommand(
                        param => _= UserAction());
                }
                return _btnCommand;
            }
        }

        private string _btnText;
        public string BtnText
        {
            get { return _btnText ??= Properties.Resources.TextStart; }
            set
            {
                _btnText = value;
                OnPropertyChanged("BtnText");
            }
        }

        private async Task UserAction()
        {
            if (!_isRunning)
            {
                _isRunning = true;
                importFlag = AutoImportCheck ? LibraryTileType.multiImport : LibraryTileType.processing;
                //BtnCommand.RaiseCanExecuteChanged();
                BtnText = Properties.Resources.TextStop;

                try
                {
                    await WallpaperInstallZip(cancellationTokenSrc.Token);
                    SelectedItem = null;
                    SetWallpaper();
                }
                catch (OperationCanceledException)
                {
                    Logger.Info("Wallpaper import cancelled by user.");
                }
            }
            else
            {
                CloseAction();
            }
        }

        private async Task WallpaperInstallZip(CancellationToken cancellationToken)
        {
            //start by extracting all Lively .zip files.
            var zipItems = ListItems.Where(x => x.Type == (WallpaperType)100).ToList();
            for (int i = 0; i < zipItems.Count; i++)
            {
                SelectedItem = zipItems[i];
                await Program.LibraryVM.WallpaperInstall(zipItems[i].Path, false);
                ListItems.Remove(zipItems[i]);
                Progress = 100 * (totalItems - ListItems.Count) / totalItems;

                cancellationToken.ThrowIfCancellationRequested();
            }
        }

        private void SetupDesktop_WallpaperChanged(object sender, EventArgs e)
        {
            SetWallpaper();
        }

        private void SetWallpaper()
        {
            if (ListItems.Count > 0)
            {
                if (SelectedItem == ListItems[0])
                {
                    ListItems.RemoveAt(0);
                    if (ListItems.Count == 0)
                    {
                        Progress = 100f;
                        CloseAction();
                        return;
                    }
                }

                if (!_isRunning)
                    return;

                SelectedItem = ListItems[0];
                Program.LibraryVM.AddWallpaper(SelectedItem.Path,
                    SelectedItem.Type,
                    importFlag,
                    Program.SettingsVM.Settings.SelectedDisplay);
                Progress = 100 * (totalItems - ListItems.Count) / totalItems;
            }
        }

        public void OnWindowClosing(object sender, CancelEventArgs e)
        {
            if (_isRunning)
            {
                cancellationTokenSrc.Cancel();
            }
            SetupDesktop.WallpaperChanged -= SetupDesktop_WallpaperChanged;
        }

        public Action CloseAction { get; set; }
    }
}
