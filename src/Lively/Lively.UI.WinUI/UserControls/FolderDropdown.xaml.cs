using CommunityToolkit.Mvvm.Input;
using Lively.Common;
using Lively.Common.Helpers.Files;
using Lively.Common.Helpers.Shell;
using Lively.Models.Enums;
using Lively.Models.UserControls;
using Lively.UI.WinUI.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Windows.Storage;
using Windows.Storage.Pickers;

namespace Lively.UI.WinUI.UserControls
{
    public sealed partial class FolderDropdown : UserControl
    {
        public string FolderName
        {
            get { return (string)GetValue(FolderNameProperty); }
            set { SetValue(FolderNameProperty, value); }
        }

        public static readonly DependencyProperty FolderNameProperty =
            DependencyProperty.Register("FolderName", typeof(string), typeof(FolderDropdown), new PropertyMetadata(null, OnRequiredDependencyPropertyChanged));

        public string FileName
        {
            get { return (string)GetValue(FileNameProperty); }
            set { SetValue(FileNameProperty, value); }
        }

        public static readonly DependencyProperty FileNameProperty =
            DependencyProperty.Register("FileName", typeof(string), typeof(FolderDropdown), new PropertyMetadata(null, OnRequiredDependencyPropertyChanged));

        public string Filter
        {
            get { return (string)GetValue(FilterProperty); }
            set { SetValue(FilterProperty, value); }
        }

        public static readonly DependencyProperty FilterProperty =
            DependencyProperty.Register("Filter", typeof(string), typeof(FolderDropdown), new PropertyMetadata(null, OnRequiredDependencyPropertyChanged));

        public string ParentFolderPath
        {
            get { return (string)GetValue(ParentFolderPathProperty); }
            set { SetValue(ParentFolderPathProperty, value); }
        }

        public static readonly DependencyProperty ParentFolderPathProperty =
            DependencyProperty.Register("ParentFolderPath", typeof(string), typeof(FolderDropdown), new PropertyMetadata(null, OnRequiredDependencyPropertyChanged));

        public ObservableCollection<FolderDropdownUserControlModel> Files
        {
            get { return (ObservableCollection<FolderDropdownUserControlModel>)GetValue(FilesProperty); }
            private set { SetValue(FilesProperty, value); }
        }

        public static readonly DependencyProperty FilesProperty =
            DependencyProperty.Register("Files", typeof(ObservableCollection<FolderDropdownUserControlModel>), typeof(FolderDropdown), new PropertyMetadata(null));

        public FolderDropdownUserControlModel SelectedFile
        {
            get { return (FolderDropdownUserControlModel)GetValue(SelectedFileProperty); }
            private set { SetValue(SelectedFileProperty, value); }
        }

        public static readonly DependencyProperty SelectedFileProperty =
            DependencyProperty.Register("SelectedFile", typeof(FolderDropdownUserControlModel), typeof(FolderDropdown), new PropertyMetadata(null, OnSelectedFilePropertyChanged));

        public ICommand Command
        {
            get { return (ICommand)GetValue(CommandProperty); }
            set { SetValue(CommandProperty, value); }
        }

        public static readonly DependencyProperty CommandProperty =
            DependencyProperty.Register("Command", typeof(ICommand), typeof(FolderDropdown), new PropertyMetadata(null));

        public object CommandParameter
        {
            get { return GetValue(CommandParameterProperty); }
            set { SetValue(CommandParameterProperty, value); }
        }

        public static readonly DependencyProperty CommandParameterProperty =
            DependencyProperty.Register("CommandParameter", typeof(object), typeof(FolderDropdown), new PropertyMetadata(null));

        public RelayCommand OpenFileCommand { get; }

        private static void OnRequiredDependencyPropertyChanged(DependencyObject s, DependencyPropertyChangedEventArgs e)
        {
            var obj = s as FolderDropdown;
            if (e.Property == FileNameProperty)
                obj.FileName = (string)e.NewValue;
            else if (e.Property == FolderNameProperty)
                obj.FolderName = (string)e.NewValue;
            else if (e.Property == FilterProperty)
                obj.Filter = (string)e.NewValue;
            else if (e.Property == ParentFolderPathProperty)
                obj.ParentFolderPath = (string)e.NewValue;

            // FileName can be null, not selected or file not found.
            // ParentFolderPath is viewmodel ElementName binding, happens after this.Loaded control event.
            obj.isRequiredPropertiesInitialized = obj.FolderName != null && obj.Filter != null && obj.ParentFolderPath != null;
            if (obj.isRequiredPropertiesInitialized)
                obj.InitializeControl();
        }

        private static void OnSelectedFilePropertyChanged(DependencyObject s, DependencyPropertyChangedEventArgs e)
        {
            var obj = s as FolderDropdown;
            // See Flyout_Opened, when loading NULL (default) value followed by binded value is fired.
            if (obj.isFlyoutContentLoading)
                return;

            obj.FileName = (e?.NewValue as FolderDropdownUserControlModel)?.FileName;
            obj.Command?.Execute(obj.CommandParameter);
        }

        private readonly IEnumerable<string> imageExtensions;
        private string cacheDir;

        private bool isInitialized;
        private bool isFlyoutContentLoading;
        private bool isRequiredPropertiesInitialized;

        public FolderDropdown()
        {
            this.InitializeComponent();
            OpenFileCommand = new RelayCommand(OpenFile);
            OpenFileCommand.CanExecute(isInitialized);
            this.imageExtensions = FileTypes.SupportedFormats.Where(x => x.Type == WallpaperType.picture || x.Type == WallpaperType.gif)
                                                             .SelectMany(x => x.Extentions);
        }

        private void InitializeControl()
        {
            if (isInitialized)
                return;

            isInitialized = true;
            OpenFileCommand.NotifyCanExecuteChanged();
            var folderPath = Path.Combine(ParentFolderPath, FolderName);
            var filePath = !string.IsNullOrWhiteSpace(FileName) ? Path.Combine(ParentFolderPath, FolderName, FileName) : null;
            if (!Directory.Exists(folderPath))
                Directory.CreateDirectory(folderPath);

            // Setup cache.
            // LivelyInfo.json root folder, this is unique.
            var rootFolderName = Path.GetFileName(Path.GetDirectoryName(folderPath));
            cacheDir = Path.Combine(Constants.CommonPaths.TempDir, "folderDropdown", rootFolderName);
            Directory.CreateDirectory(cacheDir);

            // Update items.
            Files ??= [];
            foreach (var item in FileUtil.GetFiles(folderPath, Filter, SearchOption.TopDirectoryOnly))
                Files.Add(CreateModel(item));

            // Update selection.
            // If file not found then select next available file, this is different from old behaviour before file deletion support.
            // We don't do this behaviour in wallpaper's restore function, should be fine as long as files are only managed from here.
            SelectedFile = File.Exists(filePath) ?
                Files.FirstOrDefault(x => x.FileName == Path.GetFileName(filePath)) : Files.FirstOrDefault();

        }

        private void Delete_Button_Click(object sender, RoutedEventArgs e)
        {
            var s = sender as Button;
            var obj = s.DataContext as FolderDropdownUserControlModel;

            // Make selection here before deletion to avoid sending NULL.
            if (obj == SelectedFile)
            {
                var index = Files.IndexOf(obj);
                if (index >= 0 && index < Files.Count - 1)
                {
                    // Select next item if not last.
                    SelectedFile = Files[index + 1];
                }
                else if (index == Files.Count - 1 && Files.Count > 1)
                {
                    // Select previous item if last.
                    SelectedFile = Files[index - 1];
                }
            }

            try
            {
                Files.Remove(obj);
                File.Delete(obj.FilePath);
                DeleteThumbnailTempCache(obj.FilePath);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
            }
        }

        private async void OpenFile()
        {
            if (!isInitialized)
                return;

            var selectedFiles = await OpenMultipleFilePicker(Filter);
            if (selectedFiles.Count == 0) 
                return;

            var destFiles = new List<string>();
            var destFolder = Path.Combine(ParentFolderPath, FolderName);
            // Copy the new file over.
            foreach (var srcFile in selectedFiles)
            {
                var destFile = Path.Combine(destFolder, Path.GetFileName(srcFile.Path));
                if (!File.Exists(destFile))
                {
                    File.Copy(srcFile.Path, destFile);
                }
                else
                {
                    destFile = FileUtil.NextAvailableFilename(destFile);
                    File.Copy(srcFile.Path, destFile);
                }
                destFiles.Add(destFile);
            }
            // Add copied files to bottom of dropdown..
            foreach (var file in destFiles.OrderBy(x => Path.GetFileName(x)))
            {
                Files.Add(CreateModel(file));
            }
            // Select the new file based on either:
            // -> Single file chosen.
            // -> Multiple file chosen and no file currently selected.
            if (selectedFiles.Count == 1)
                SelectedFile = Files[Files.Count - 1];
            else if (SelectedFile is null)
                SelectedFile = Files[Files.Count - 1];
        }

        // Workaround: Crashing at times when opening flyout
        // Ref: https://github.com/microsoft/microsoft-ui-xaml/issues/8412
        private void Flyout_Opened(object sender, object e)
        {
            this.DispatcherQueue.TryEnqueue(() =>
            {
                try
                {
                    isFlyoutContentLoading = true;
                    this.FindName("listView");
                }
                finally
                {
                    isFlyoutContentLoading = false;
                }
            });
        }

        private static async Task<IReadOnlyList<StorageFile>> OpenMultipleFilePicker(string folderDropDownFilter)
        {
            var filePicker = new FileOpenPicker();
            filePicker.SetOwnerWindow(App.StartFlags.TrayWidget ? new Window() : App.Services.GetRequiredService<MainWindow>());
            var filterString = folderDropDownFilter;
            var filter = filterString == "*" ? new string[] { "*" } : filterString.Replace("*", string.Empty).Split("|");
            foreach (var item in filter)
            {
                filePicker.FileTypeFilter.Add(item);
            }
            return await filePicker.PickMultipleFilesAsync();
        }

        private FolderDropdownUserControlModel CreateModel(string file)
        {
            var fileName = Path.GetFileName(file);
            var isImage = imageExtensions.Contains(Path.GetExtension(file), StringComparer.OrdinalIgnoreCase);

            return new FolderDropdownUserControlModel
            {
                ImagePath = isImage ? file : GetThumbnailTempCache(file, 128),
                FileName = fileName,
                FilePath = file
            };
        }

        private string GetThumbnailTempCache(string filePath, int thumbnailSize)
        {
            if (cacheDir is null)
                return null;

            var fileName = Path.GetFileName(filePath);
            var cacheFile = Path.Combine(cacheDir, fileName + ".png");

            using var thumbnail = GetThumbnail(filePath, thumbnailSize);
            if (!File.Exists(cacheFile))
                thumbnail.Save(cacheFile, System.Drawing.Imaging.ImageFormat.Png);

            return cacheFile;
        }

        private void DeleteThumbnailTempCache(string filePath)
        {
            if (cacheDir is null)
                return;

            var fileName = Path.GetFileName(filePath);
            var cacheFilePath = Path.Combine(cacheDir, fileName + ".png");
            if (File.Exists(cacheFilePath))
                File.Delete(cacheFilePath);
        }

        /// <summary>
        /// Retrieve system thumbnail, if not found file association icon is required.
        /// </summary>
        private static Bitmap GetThumbnail(string filePath, int thumbnailSize)
        {
            // System.Drawing.Icon.ExtractAssociatedIcon can also be used if only file association icon is required.
            return ThumbnailUtil.GetThumbnail(
               filePath, thumbnailSize, thumbnailSize, ThumbnailUtil.ThumbnailOptions.None);
        }
    }
}
