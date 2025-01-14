using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ImageMagick;
using Lively.Common;
using Lively.Common.Helpers;
using Lively.Common.Helpers.Archive;
using Lively.Common.Helpers.Files;
using Lively.Common.Helpers.Storage;
using Lively.Common.Services;
using Lively.Grpc.Client;
using Lively.ML.DepthEstimate;
using Lively.ML.Helpers;
using Lively.Models;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Lively.UI.Shared.ViewModels
{
    public partial class DepthEstimateWallpaperViewModel : ObservableObject
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
        public LibraryModel NewWallpaper { get; private set; }
        public event EventHandler OnRequestClose;

        private readonly IResourceService i18n;
        private readonly string modelPath = Path.Combine(Constants.MachineLearning.MiDaSDir, "model.onnx");
        private readonly string templateDir = Path.Combine(Constants.MachineLearning.MiDaSDir, "Templates", "0");
        private CancellationTokenSource downloadCts;

        private readonly IDepthEstimate depthEstimate;
        private readonly IDownloadService downloader;
        private readonly LibraryViewModel libraryVm;
        private readonly IUserSettingsClient userSettings;
        private readonly IDispatcherService dispatcher;
        private readonly IDesktopCoreClient desktopCore;

        public DepthEstimateWallpaperViewModel(IDepthEstimate depthEstimate,
            IDownloadService downloader,
            LibraryViewModel libraryVm,
            IUserSettingsClient userSettings,
            IDispatcherService dispatcher,
            IResourceService i18n,
            IDesktopCoreClient desktopCore)
        {
            this.depthEstimate = depthEstimate;
            this.downloader = downloader;
            this.libraryVm = libraryVm;
            this.userSettings = userSettings;
            this.desktopCore = desktopCore;
            this.dispatcher = dispatcher;

            this.i18n = i18n;

            IsModelExists = CheckModel();
            CanRunCommand = IsModelExists;
            RunCommand.NotifyCanExecuteChanged();
        }

        [ObservableProperty]
        private bool isModelExists;

        [ObservableProperty]
        private bool isRunning;

        [ObservableProperty]
        private string errorText;

        [ObservableProperty]
        private string backgroundImage;

        [ObservableProperty]
        private string previewText;

        [ObservableProperty]
        private string previewImage;

        [ObservableProperty]
        private float modelDownloadProgress;

        [ObservableProperty]
        private string modelDownloadProgressText = "--/-- MB";

        private string _selectedImage;
        public string SelectedImage
        {
            get => _selectedImage;
            set
            {
                SetProperty(ref _selectedImage, value);
                BackgroundImage = value;
                PreviewImage = value;
            }
        }

        private bool CanRunCommand { get; set; } = false;

        private bool CanCancelCommand { get; set; } = true;

        private bool CanDownloadModelCommand { get; set; } = true;


        [RelayCommand(CanExecute = nameof(CanRunCommand))]
        private async Task Run()
        {
            var destDir = Path.Combine(userSettings.Settings.WallpaperDir, Constants.CommonPartialPaths.WallpaperInstallDir, Path.GetRandomFileName());
            var depthImagePath = Path.Combine(destDir, "media", "depth.jpg");
            var inputImageCopyPath = Path.Combine(destDir, "media", "image.jpg");
            var inputImagePath = SelectedImage;

            try
            {
                IsRunning = true;
                CanRunCommand = false;
                RunCommand.NotifyCanExecuteChanged();
                CanCancelCommand = false;
                CancelCommand.NotifyCanExecuteChanged();
                PreviewText = i18n.GetString("DescriptionDepthApprox/Content");

                await Task.Run(async() =>
                {
                    using var inputImage = new MagickImage(inputImagePath);
                    inputImage.Strip(); //Remove metadata
                    //Resize input for performance and memory
                    if (inputImage.Width > 3840 || inputImage.Height > 3840)
                    {
                        //Fit the image within aspect ratio, if width > height = 3840x.. else ..x3840
                        //ref: https://legacy.imagemagick.org/Usage/resize/
                        inputImage.Resize(new MagickGeometry()
                        {
                            Width = 3840,
                            Height = 3840,
                            IgnoreAspectRatio = false,
                        });
                    }

                    if (!modelPath.Equals(depthEstimate.ModelPath, StringComparison.Ordinal))
                        depthEstimate.LoadModel(modelPath);
                    var depthOutput = depthEstimate.Run(inputImagePath);
                    //Resize depth to same size as input
                    using var depthImage = ImageUtil.FloatArrayToMagickImage(depthOutput.Depth, depthOutput.Width, depthOutput.Height);
                    depthImage.Resize(new MagickGeometry(inputImage.Width, inputImage.Height) { IgnoreAspectRatio = true });

                    //Create wallpaper from template
                    FileUtil.DirectoryCopy(templateDir, destDir, true);
                    await inputImage.WriteAsync(inputImageCopyPath);
                    await depthImage.WriteAsync(depthImagePath);
                    //Generate wallpaper metadata
                    inputImage.Thumbnail(new MagickGeometry()
                    {
                        Width = 480,
                        Height = 270,
                        IgnoreAspectRatio = false,
                        FillArea = true
                    });
                    inputImage.Extent(480, 270, Gravity.Center);
                    await inputImage.WriteAsync(Path.Combine(destDir, "thumbnail.jpg"));
                    //LivelyInfo.json update
                    var infoModel = JsonStorage<LivelyInfoModel>.LoadData(Path.Combine(destDir, "LivelyInfo.json"));
                    infoModel.Title = Path.GetFileNameWithoutExtension(inputImagePath);
                    infoModel.Desc = i18n.GetString("DescriptionDepthWallpaperTemplate/Content");
                    infoModel.AppVersion = desktopCore.AssemblyVersion.ToString();
                    JsonStorage<LivelyInfoModel>.StoreData(Path.Combine(destDir, "LivelyInfo.json"), infoModel);
                });

                //Preview output to user
                await Task.Delay(500);
                PreviewImage = depthImagePath;
                PreviewText = i18n.GetString("TextCompleted");
                await Task.Delay(1500);
                //Install wallpaper and close dialog
                NewWallpaper = libraryVm.AddWallpaperFolder(destDir);
                OnRequestClose?.Invoke(this, EventArgs.Empty);
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                ErrorText = $"{i18n.GetString("TextError")}: {ex.Message}";
                PreviewText = string.Empty;

                await FileUtil.TryDeleteDirectoryAsync(destDir, 0, 1000);
            }
            finally
            {
                IsRunning = false;
                CanCancelCommand = true;
                CancelCommand.NotifyCanExecuteChanged();
            }
        }

        [RelayCommand(CanExecute = nameof(CanDownloadModelCommand))]
        private async Task DownloadModel()
        {
            try
            {
                CanDownloadModelCommand = false;
                DownloadModelCommand.NotifyCanExecuteChanged();

                var uri = await GetModelUrl();
                Directory.CreateDirectory(Constants.MachineLearning.MiDaSDir);
                var tempPath = Path.Combine(Constants.CommonPaths.TempDir, Path.GetRandomFileName() + ".zip");
                downloadCts = new CancellationTokenSource();

                await downloader.DownloadFile(uri, tempPath, new Progress<(double downloaded, double total)>(progress =>
                {
                    ModelDownloadProgressText = $"{progress.downloaded}/{progress.total} MB";
                    ModelDownloadProgress = (float)(progress.downloaded * 100 / progress.total);
                }), downloadCts.Token);

                if (!downloadCts.Token.IsCancellationRequested)
                {
                    await Task.Run(() => ZipExtract.ZipExtractFile(tempPath, Constants.MachineLearning.MiDaSDir, false));
                    IsModelExists = CheckModel();
                    BackgroundImage = IsModelExists ? SelectedImage : BackgroundImage;
                }

                //try
                //{
                //    File.Delete(tempPath);
                //}
                //catch { }

                CanRunCommand = IsModelExists;
                RunCommand.NotifyCanExecuteChanged();
            }
            catch(Exception ex)
            {
                Logger.Error(ex);
                ErrorText = $"{i18n.GetString("TextError")}: {ex.Message}";
            }
            //finally
            //{
            //    _canDownloadModelCommand = true;
            //    DownloadModelCommand.NotifyCanExecuteChanged();
            //}
        }

        [RelayCommand(CanExecute = nameof(CanCancelCommand))]
        private void Cancel()
        {
            downloadCts?.Cancel();
        }

        private static async Task<Uri> GetModelUrl()
        {
            var userName = "rocksdanister";
            var repositoryName = "lively-ml-models";
            var gitRelease = await GithubUtil.GetLatestRelease(userName, repositoryName);
            var (_, Url) = GithubUtil.GetAssetUrl(gitRelease, "midas_small.zip");

            return new Uri(Url);
        }

        private bool CheckModel() => File.Exists(modelPath);
    }
}
