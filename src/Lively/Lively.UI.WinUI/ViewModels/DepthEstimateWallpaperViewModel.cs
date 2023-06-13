using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Lively.Common;
using Lively.ML.DepthEstimate;
using Lively.ML.Helpers;
using Lively.Models;
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

        public DepthEstimateWallpaperViewModel(IDepthEstimate depthEstimate)
        {
            this.depthEstimate = depthEstimate;

            _canRunCommand = IsModelExists;
            RunCommand.NotifyCanExecuteChanged();

            BackgroundImage = IsModelExists ? string.Empty : "ms-appx:///Assets/banner-lively-1080.jpg";
        }

        [ObservableProperty]
        private bool isModelExists = CheckModel();

        [ObservableProperty]
        private bool isRunning;

        [ObservableProperty]
        private string backgroundImage;

        [ObservableProperty]
        private string previewText = "---";

        [ObservableProperty]
        private string previewImage;

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
        public RelayCommand RunCommand => _runCommand ??= new RelayCommand(async() => await PredictDepth(), () => _canRunCommand);

        private RelayCommand _downloadModelCommand;
        public RelayCommand DownloadModelCommand => _downloadModelCommand ??= new RelayCommand(() => DownloadModel());

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

        private void DownloadModel()
        {
            throw new NotImplementedException();
        }

        private static bool CheckModel() => File.Exists(Constants.MachineLearning.MiDaSPath);
    }
}
