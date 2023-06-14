using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Downloader;
using Lively.Common;
using Lively.Common.Helpers;
using Lively.Common.Helpers.Files;
using Lively.Common.Helpers.Network;
using Lively.Grpc.Client;
using Lively.ML.DepthEstimate;
using Lively.ML.Helpers;
using Lively.Models;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lively.UI.WinUI.ViewModels
{
    public partial class DepthEstimateWallpaperViewModel : ObservableObject
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
        public event EventHandler OnRequestClose;

        private readonly IDepthEstimate depthEstimate;
        private readonly IDownloadHelper downloader;
        private readonly LibraryViewModel libraryVm;
        private readonly IUserSettingsClient userSettings;
        private readonly IDesktopCoreClient desktopCore;

        public DepthEstimateWallpaperViewModel(IDepthEstimate depthEstimate,
            IDownloadHelper downloader,
            LibraryViewModel libraryVm, 
            IUserSettingsClient userSettings,
            IDesktopCoreClient desktopCore)
        {
            this.depthEstimate = depthEstimate;
            this.downloader = downloader;
            this.libraryVm = libraryVm;
            this.userSettings = userSettings;
            this.desktopCore = desktopCore;

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

        private async Task PredictDepth()
        {
            try
            {
                IsRunning = true;
                _canRunCommand = false;
                RunCommand.NotifyCanExecuteChanged();
                PreviewText = "Approximating depth..";

                //single use, otherwise don't reload model everytime
                depthEstimate.LoadModel(Constants.MachineLearning.MiDaSPath);
                var output = depthEstimate.Run(SelectedImage);
                await Task.Delay(1500);

                using var img = ImageUtil.FloatArrayToMagickImageResize(output.Depth, output.Width, output.Height, output.OriginalWidth, output.OriginalHeight);
                var tempImgPath = Path.Combine(Constants.CommonPaths.TempDir, Path.GetRandomFileName() + ".jpg");
                img.Write(tempImgPath);
                PreviewImage = tempImgPath;
                PreviewText = "Completed";
                await Task.Delay(1500);

                await CreateWallpaper();
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

        private async Task CreateWallpaper()
        {
            var srcDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "WallpaperTemplates", "depthmap");
            var destDir = Path.Combine(userSettings.Settings.WallpaperDir, Constants.CommonPartialPaths.WallpaperInstallDir, Path.GetRandomFileName());
            FileOperations.DirectoryCopy(srcDir, destDir, true);
            File.Copy(PreviewImage, Path.Combine(destDir, "media", "depth.jpg"), true);
            File.Copy(SelectedImage, Path.Combine(destDir, "media", "image.jpg"), true); //todo: if not jpeg?

            var item = libraryVm.AddWallpaperFolder(destDir);
            await desktopCore.SetWallpaper(item, userSettings.Settings.SelectedDisplay);
        }

        //public async Task Close()
        //{
        //    await CreateWallpaper();
        //    OnRequestClose?.Invoke(this, EventArgs.Empty);
        //}

        private async Task DownloadModel()
        {
            _canDownloadModelCommand = false;
            DownloadModelCommand.NotifyCanExecuteChanged();

            var uri = await GetModelUrl();
            downloader.DownloadFile(uri, Constants.MachineLearning.MiDaSPath);
            downloader.DownloadStarted += (s, e) => 
            {
                _ = App.Services.GetRequiredService<MainWindow>().DispatcherQueue.TryEnqueue(() =>
                {
                    ModelDownloadProgressText = $"0/{e.TotalSize}MB";
                });
            };
            downloader.DownloadProgressChanged += (s, e) =>
            {
                _ = App.Services.GetRequiredService<MainWindow>().DispatcherQueue.TryEnqueue(() =>
                {
                    ModelDownloadProgressText = $"{e.DownloadedSize}/{e.TotalSize}MB";
                    ModelDownloadProgress = (float)e.Percentage;
                });
            };
            downloader.DownloadFileCompleted += (s, e) =>
            {
                _ = App.Services.GetRequiredService<MainWindow>().DispatcherQueue.TryEnqueue(() =>
                {
                    IsModelExists = CheckModel();
                    BackgroundImage = IsModelExists ? SelectedImage : BackgroundImage;

                    _canRunCommand = IsModelExists;
                    RunCommand.NotifyCanExecuteChanged();
                });
            };
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
