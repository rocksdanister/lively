using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ImageMagick;
using Lively.Common;
using Lively.Common.Helpers;
using Lively.Common.Helpers.Archive;
using Lively.Common.Helpers.Files;
using Lively.Common.Helpers.Network;
using Lively.Common.Helpers.Storage;
using Lively.Grpc.Client;
using Lively.ML.DepthEstimate;
using Lively.ML.Helpers;
using Lively.Models;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;
using Windows.ApplicationModel.Resources;

namespace Lively.UI.WinUI.ViewModels
{
    public partial class DepthEstimateWallpaperViewModel : ObservableObject
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
        public ILibraryModel NewWallpaper { get; private set; }
        public event EventHandler OnRequestClose;

        private readonly ResourceLoader i18n;
        private readonly string modelPath = Path.Combine(Constants.MachineLearning.MiDaSDir, "model.onnx");
        private readonly string templateDir = Path.Combine(Constants.MachineLearning.MiDaSDir, "Templates", "0");

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

            i18n = ResourceLoader.GetForViewIndependentUse();

            IsModelExists = CheckModel();
            _canRunCommand = IsModelExists;
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

        private bool _canRunCommand = false;
        private RelayCommand _runCommand;
        public RelayCommand RunCommand => _runCommand ??= new RelayCommand(async() => await CreateDepthWallpaper(), () => _canRunCommand);

        private bool _canDownloadModelCommand = true;
        private RelayCommand _downloadModelCommand;
        public RelayCommand DownloadModelCommand => _downloadModelCommand ??= new RelayCommand(async() => await DownloadModel(), () => _canDownloadModelCommand);

        private bool _canCancelCommand = true;
        private RelayCommand _cancelCommand;
        public RelayCommand CancelCommand => _cancelCommand ??= new RelayCommand(CancelOperations, () => _canCancelCommand);

        private async Task CreateDepthWallpaper()
        {
            var destDir = Path.Combine(userSettings.Settings.WallpaperDir, Constants.CommonPartialPaths.WallpaperInstallDir, Path.GetRandomFileName());
            var depthImagePath = Path.Combine(destDir, "media", "depth.jpg");
            var inputImageCopyPath = Path.Combine(destDir, "media", "image.jpg");
            var inputImagePath = SelectedImage;

            try
            {
                IsRunning = true;
                _canRunCommand = false;
                RunCommand.NotifyCanExecuteChanged();
                _canCancelCommand = false;
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
                    FileOperations.DirectoryCopy(templateDir, destDir, true);
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

                await FileOperations.DeleteDirectoryAsync(destDir, 0, 1000);
            }
            finally
            {
                IsRunning = false;
                _canCancelCommand = true;
                CancelCommand.NotifyCanExecuteChanged();
            }
        }

        private async Task DownloadModel()
        {
            try
            {
                _canDownloadModelCommand = false;
                DownloadModelCommand.NotifyCanExecuteChanged();

                var uri = await GetModelUrl();
                Directory.CreateDirectory(Constants.MachineLearning.MiDaSDir);
                var tempPath = Path.Combine(Constants.CommonPaths.TempDir, Path.GetRandomFileName() + ".zip");
                downloader.DownloadProgressChanged += (s, e) =>
                {
                    _ = App.Services.GetRequiredService<MainWindow>().DispatcherQueue.TryEnqueue(() =>
                    {
                        ModelDownloadProgressText = $"{e.DownloadedSize}/{e.TotalSize} MB";
                        ModelDownloadProgress = (float)e.Percentage;
                    });
                };
                downloader.DownloadFileCompleted += (s, success) =>
                {
                    _ = App.Services.GetRequiredService<MainWindow>().DispatcherQueue.TryEnqueue(async () =>
                    {
                        if (success)
                        {
                            await Task.Run(() => ZipExtract.ZipExtractFile(tempPath, Constants.MachineLearning.MiDaSDir, false));
                            IsModelExists = CheckModel();
                            BackgroundImage = IsModelExists ? SelectedImage : BackgroundImage;

                            //try
                            //{
                            //    File.Delete(tempPath);
                            //}
                            //catch { }

                            _canRunCommand = IsModelExists;
                            RunCommand.NotifyCanExecuteChanged();
                        }
                        else
                            ErrorText = $"{i18n.GetString("TextError")}: Download failed.";
                    });
                };

                await downloader.DownloadFile(uri, tempPath);
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

        private void CancelOperations()
        {
            downloader?.Cancel();
        }

        private static async Task<Uri> GetModelUrl()
        {
            var userName = "rocksdanister";
            var repositoryName = "lively-ml-models";
            var gitRelease = await GithubUtil.GetLatestRelease(repositoryName, userName, 0);

            var gitUrl = await GithubUtil.GetAssetUrl("midas_small.zip", gitRelease, repositoryName, userName);
            var uri = new Uri(gitUrl);

            return uri;
        }

        private bool CheckModel() => File.Exists(modelPath);
    }
}
