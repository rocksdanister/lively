using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ImageMagick;
using Lively.Common;
using Lively.Common.Helpers;
using Lively.Common.Helpers.Files;
using Lively.Common.Helpers.Network;
using Lively.Common.Helpers.Storage;
using Lively.Grpc.Client;
using Lively.ML.DepthEstimate;
using Lively.ML.Helpers;
using Lively.Models;
using Microsoft.UI.Dispatching;
using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Windows.ApplicationModel.Resources;

namespace Lively.UI.WinUI.ViewModels
{
    public partial class DepthEstimateWallpaperViewModel : ObservableObject
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
        public ILibraryModel NewWallpaper { get; private set; }
        public event EventHandler OnRequestClose;
        private readonly DispatcherQueue dispatcherQueue;
        private readonly ResourceLoader i18n;

        private readonly IDepthEstimate depthEstimate;
        private readonly IDownloadHelper downloader;
        private readonly LibraryViewModel libraryVm;
        private readonly IUserSettingsClient userSettings;

        public DepthEstimateWallpaperViewModel(IDepthEstimate depthEstimate,
            IDownloadHelper downloader,
            LibraryViewModel libraryVm,
            IUserSettingsClient userSettings)
        {
            this.depthEstimate = depthEstimate;
            this.downloader = downloader;
            this.libraryVm = libraryVm;
            this.userSettings = userSettings;

            dispatcherQueue = DispatcherQueue.GetForCurrentThread() ?? DispatcherQueueController.CreateOnCurrentThread().DispatcherQueue;
            i18n = ResourceLoader.GetForViewIndependentUse();

            _canRunCommand = IsModelExists;
            RunCommand.NotifyCanExecuteChanged();
        }

        [ObservableProperty]
        private bool isModelExists = CheckModel();

        [ObservableProperty]
        private bool isRunning;

        [ObservableProperty]
        private string backgroundImage;

        [ObservableProperty]
        private string previewText;

        [ObservableProperty]
        private string previewImage;

        [ObservableProperty]
        private float modelDownloadProgress;

        [ObservableProperty]
        private string modelDownloadProgressText = "--/--MB";

        private string _selectedImage;
        public string SelectedImage
        {
            get => _selectedImage;
            set
            {
                SetProperty(ref _selectedImage, value);
                BackgroundImage = IsModelExists ? value : "ms-appx:///Assets/banner-lively-1080.jpg";
                PreviewImage = value;
            }
        }

        private bool _canRunCommand = false;
        private RelayCommand _runCommand;
        public RelayCommand RunCommand => _runCommand ??= new RelayCommand(async() => await PredictDepth(), () => _canRunCommand);

        private bool _canDownloadModelCommand = true;
        private RelayCommand _downloadModelCommand;
        public RelayCommand DownloadModelCommand => _downloadModelCommand ??= new RelayCommand(async() => await DownloadModel(), () => _canDownloadModelCommand);

        //private RelayCommand _cancelCommand;
        //public RelayCommand CancelCommand => _cancelCommand ??= new RelayCommand(CancelOperations);

        private async Task PredictDepth()
        {
            try
            {
                IsRunning = true;
                _canRunCommand = false;
                RunCommand.NotifyCanExecuteChanged();
                PreviewText = i18n.GetString("DescriptionDepthApprox/Content");

                if (!Constants.MachineLearning.MiDaSPath.Equals(depthEstimate.ModelPath, StringComparison.Ordinal))
                    depthEstimate.LoadModel(Constants.MachineLearning.MiDaSPath);
                var output = depthEstimate.Run(SelectedImage);
                await Task.Delay(1500);

                using var img = ImageUtil.FloatArrayToMagickImageResize(output.Depth, output.Width, output.Height, output.OriginalWidth, output.OriginalHeight);
                var tempImgPath = Path.Combine(Constants.CommonPaths.TempDir, Path.GetRandomFileName() + ".jpg");
                img.Write(tempImgPath);
                PreviewImage = tempImgPath;
                PreviewText = i18n.GetString("TextCompleted");
                await Task.Delay(1500);

                NewWallpaper = await CreateWallpaper();
                OnRequestClose?.Invoke(this, EventArgs.Empty);
            }
            catch (Exception e)
            {
                Logger.Error(e);
                PreviewText = $"Error: {e.Message}";
            }
            finally
            {
                IsRunning = false;
            }
        }

        private async Task<ILibraryModel> CreateWallpaper()
        {
            var srcDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "WallpaperTemplates", "depthmap");
            var destDir = Path.Combine(userSettings.Settings.WallpaperDir, Constants.CommonPartialPaths.WallpaperInstallDir, Path.GetRandomFileName());
            FileOperations.DirectoryCopy(srcDir, destDir, true);

            using var img = new MagickImage(SelectedImage);
            if (Path.GetExtension(SelectedImage) != ".jpg")
                await img.WriteAsync(Path.Combine(destDir, "media", "image.jpg"));
            else
                File.Copy(SelectedImage, Path.Combine(destDir, "media", "image.jpg"));
            File.Copy(PreviewImage, Path.Combine(destDir, "media", "depth.jpg"), true);

            //metadata
            img.Resize(new MagickGeometry()
            {
                Width = 480,
                Height = 270,
                IgnoreAspectRatio = false,
                FillArea = false
            });
            await img.WriteAsync(Path.Combine(destDir, "thumbnail.jpg"));
            JsonStorage<ILivelyInfoModel>.StoreData(Path.Combine(destDir, "LivelyInfo.json"), new LivelyInfoModel()
            {
                Title = Path.GetFileNameWithoutExtension(SelectedImage),
                Desc = i18n.GetString("DescriptionDepthWallpaperTemplate/Content"),
                Type = WallpaperType.web,
                IsAbsolutePath = false,
                FileName = "index.html",
                Contact = "https://github.com/rocksdanister/depthmap-wallpaper",
                License = "See License.txt",
                Author = "rocksdanister",
                AppVersion = "2.0.6.6",
                Preview = "preview.gif",
                Thumbnail = "thumbnail.jpg",
                Arguments = string.Empty,
            });

            return libraryVm.AddWallpaperFolder(destDir);
        }

        private async Task DownloadModel()
        {
            _canDownloadModelCommand = false;
            DownloadModelCommand.NotifyCanExecuteChanged();

            var uri = await GetModelUrl();
            Directory.CreateDirectory(Constants.MachineLearning.MiDaSDir);
            var tempPath = Path.Combine(Constants.CommonPaths.TempDir, Path.GetRandomFileName());
            downloader.DownloadStarted += (s, e) =>
            {
                _ = dispatcherQueue.TryEnqueue(() =>
                {
                    ModelDownloadProgressText = $"0/{e.TotalSize}MB";
                });
            };
            downloader.DownloadProgressChanged += (s, e) =>
            {
                _ = dispatcherQueue.TryEnqueue(() =>
                {
                    ModelDownloadProgressText = $"{e.DownloadedSize}/{e.TotalSize}MB";
                    ModelDownloadProgress = (float)e.Percentage;
                });
            };
            downloader.DownloadFileCompleted += (s, success) =>
            {
                _ = dispatcherQueue.TryEnqueue(async() =>
                {
                    if (success)
                    {
                        await FileOperations.CopyFileAsync(tempPath, Constants.MachineLearning.MiDaSPath);
                        IsModelExists = CheckModel();
                        BackgroundImage = IsModelExists ? SelectedImage : BackgroundImage;

                        _canRunCommand = IsModelExists;
                        RunCommand.NotifyCanExecuteChanged();

                        //try
                        //{
                        //    File.Delete(tempPath);
                        //}
                        //catch
                        //{
                        //    //ignore, will get deleted on restart
                        //}
                    }
                });
            };
            await downloader.DownloadFile(uri, tempPath);
        }

        private void CancelOperations()
        {
            downloader?.Cancel();
        }

        private async Task<Uri> GetModelUrl()
        {
            //test
            //manifest and update checker
            var userName = "rocksdanister";
            var repositoryName = "lively-ml-models";
            var gitRelease = await GithubUtil.GetLatestRelease(repositoryName, userName, 0);

            var gitUrl = await GithubUtil.GetAssetUrl("MiDaS_model-small.onnx",
                gitRelease, repositoryName, userName);
            var uri = new Uri(gitUrl);

            return uri;
        }

        private static bool CheckModel() => File.Exists(Constants.MachineLearning.MiDaSPath);
    }
}
