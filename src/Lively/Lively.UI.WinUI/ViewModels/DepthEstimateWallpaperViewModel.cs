using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Downloader;
using Lively.Common;
using Lively.Common.Helpers;
using Lively.Common.Helpers.Network;
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

        private readonly IDepthEstimate depthEstimate;
        private readonly IDownloadHelper downloader;

        public DepthEstimateWallpaperViewModel(IDepthEstimate depthEstimate, IDownloadHelper downloader)
        {
            this.depthEstimate = depthEstimate;
            this.downloader = downloader;

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
                PreviewText = "Getting things ready..";
           
                depthEstimate.LoadModel(Constants.MachineLearning.MiDaSPath);
                var output = depthEstimate.Run(SelectedImage);

                await Task.Delay(1500);
                PreviewText = "Approximating depth..";

                await Task.Delay(1500);
                PreviewText = "Completed";

                var img = ImageUtil.FloatArrayToMagickImageResize(output.Depth, output.Width, output.Height, output.OriginalWidth, output.OriginalHeight);
                var tempImgPath = Path.Combine(Constants.CommonPaths.TempDir, Path.GetRandomFileName() + ".jpg");
                img.Write(tempImgPath);
                PreviewImage = tempImgPath;
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

        private async Task DownloadModel()
        {
            _canDownloadModelCommand = false;
            DownloadModelCommand.NotifyCanExecuteChanged();

            var uri = await GetModelUrl();
            downloader.DownloadFile(uri, Path.Combine(Constants.CommonPaths.TempDir, "test.onnx"));
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
            var userName = "rocksdanister";
            var repositoryName = "lively-beta";
            var gitRelease = await GithubUtil.GetLatestRelease(repositoryName, userName, 0);

            var gitUrl = await GithubUtil.GetAssetUrl("MiDaS_model-small.onnx",
                gitRelease, repositoryName, userName);
            var uri = new Uri(gitUrl);

            return uri;
        }

        private static bool CheckModel() => File.Exists(Constants.MachineLearning.MiDaSPath);
    }
}
