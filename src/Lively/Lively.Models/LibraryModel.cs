using CommunityToolkit.Mvvm.ComponentModel;
using Lively.Common;
using System;
using System.ComponentModel;
using System.IO;
using Lively.Common.Extensions;

namespace Lively.Models
{
    public partial class LibraryModel : ObservableObject
    {
        public LibraryModel(LivelyInfoModel data, string folderPath, LibraryItemType tileType = LibraryItemType.ready, bool preferPreviewGif = false)
        {
            DataType = tileType;
            LivelyInfo = new LivelyInfoModel(data);
            Title = data.Title;
            Desc = data.Desc;
            Author = data.Author;

            try
            {
                SrcWebsite = LinkHandler.SanitizeUrl(data.Contact);
            }
            catch
            {
                SrcWebsite = null;
            }

            if (data.IsAbsolutePath)
            {
                //full filepath is stored in Livelyinfo.json metadata file.
                FilePath = data.FileName;

                //This is to keep backward compatibility with older wallpaper files.
                //When I originally made the property all the paths where made absolute, not just wallpaper path.
                //But previewgif and thumb are always inside the temporary lively created folder.
                try
                {
                    //PreviewClipPath = data.Preview;
                    PreviewClipPath = Path.Combine(folderPath, Path.GetFileName(data.Preview));
                }
                catch
                {
                    PreviewClipPath = null;
                }

                try
                {
                    //ThumbnailPath = data.Thumbnail;
                    ThumbnailPath = Path.Combine(folderPath, Path.GetFileName(data.Thumbnail));
                }
                catch
                {
                    ThumbnailPath = null;
                }

                try
                {
                    LivelyPropertyPath = Path.Combine(Directory.GetParent(data.FileName).ToString(), "LivelyProperties.json");
                    //LivelyPropertyPath ??= Path.Combine(folderPath, "LivelyProperties.json");
                }
                catch
                {
                    LivelyPropertyPath = null;
                }
            }
            else
            {
                //Only relative path is stored, this will be inside "Lively Wallpaper" folder.
                if (data.Type == WallpaperType.url
                || data.Type == WallpaperType.videostream)
                {
                    //no file.
                    FilePath = data.FileName;
                }
                else
                {
                    try
                    {
                        FilePath = Path.Combine(folderPath, data.FileName);
                    }
                    catch
                    {
                        FilePath = null;
                    }

                    try
                    {
                        LivelyPropertyPath = Path.Combine(folderPath, "LivelyProperties.json");
                    }
                    catch
                    {
                        LivelyPropertyPath = null;
                    }
                }

                try
                {
                    PreviewClipPath = Path.Combine(folderPath, data.Preview);
                }
                catch
                {
                    PreviewClipPath = null;
                }

                try
                {
                    ThumbnailPath = Path.Combine(folderPath, data.Thumbnail);
                }
                catch
                {
                    ThumbnailPath = null;
                }
            }

            LivelyInfoFolderPath = folderPath;
            //Use animated gif if exists.
            ImagePath = preferPreviewGif ?
                (File.Exists(PreviewClipPath) ? PreviewClipPath : ThumbnailPath) : ThumbnailPath;
            ItemStartup = false;

            if (data.Type == WallpaperType.video || data.Type == WallpaperType.videostream || data.Type == WallpaperType.gif || data.Type == WallpaperType.picture)
            {
                LivelyPropertyPath ??= Path.Combine(Constants.CommonPaths.TempVideoDir, "LivelyProperties.json");
            }

            //Assume its gallery wp if these conditions true (offline.)
            IsSubscribed = DataType == LibraryItemType.ready && !string.IsNullOrEmpty(data.Id);
        }

        public LibraryModel(LivelyInfoModel data, LibraryItemType tileType = LibraryItemType.gallery)
        {
            Title = data.Title;
            Desc = data.Desc;
            ImagePath = data.Preview ?? data.Thumbnail;
            LivelyInfo = new LivelyInfoModel(data);
            DataType = tileType;
            //IsSubscribed = !string.IsNullOrEmpty(data.Id);
        }

        [ObservableProperty]
        private bool isSubscribed;

        [ObservableProperty]
        private bool isDownloading;

        [ObservableProperty]
        private float downloadingProgress;

        [ObservableProperty]
        private string downloadingProgressText = "-/- MB";

        [ObservableProperty]
        private LivelyInfoModel livelyInfo;

        [ObservableProperty]
        private LibraryItemType dataType;

        private string _filePath;
        public string FilePath
        {
            get => _filePath;
            set
            {
                value = LivelyInfo.Type.IsOnlineWallpaper() || File.Exists(value) ? value : null;
                SetProperty(ref _filePath, value);
            }
        }

        [ObservableProperty]
        private string livelyInfoFolderPath;

        private string _title;
        public string Title
        {
            get => _title;
            set
            {
                value = string.IsNullOrWhiteSpace(value) ? "---" : (value.Length > 100 ? value.Substring(0, 100) : value);
                SetProperty(ref _title, value);
            }
        }

        private string _author;
        public string Author
        {
            get => _author;
            set
            {
                value = string.IsNullOrWhiteSpace(value) ? "---" : (value.Length > 100 ? value.Substring(0, 100) : value);
                SetProperty(ref _author, value);
            }
        }

        private string _desc;
        public string Desc
        {
            get => _desc;
            set
            {
                value = string.IsNullOrWhiteSpace(value) ? "---" : (value.Length > 5000 ? value.Substring(0, 5000) : value);
                SetProperty(ref _desc, value);
            }
        }

        [ObservableProperty]
        private string imagePath;
        
        private string _previewClipPath;
        public string PreviewClipPath
        {
            get => _previewClipPath;
            set
            {
                value = File.Exists(value) ? value : null;
                SetProperty(ref _previewClipPath, value);
            }
        }

        private string _thumbnailPath;
        public string ThumbnailPath
        {
            get => _thumbnailPath;
            set
            {
                value = File.Exists(value) ? value : null;
                SetProperty(ref _thumbnailPath, value);
            }
        }

        [ObservableProperty]
        private Uri srcWebsite;

        private string _livelyPropertyPath;
        /// <summary>
        /// LivelyProperties.json filepath if present, null otherwise.
        /// </summary>
        public string LivelyPropertyPath
        {
            get => _livelyPropertyPath;
            set
            {
                value = File.Exists(value) ? value : null;
                SetProperty(ref _livelyPropertyPath, value);
            }
        }

        [ObservableProperty]
        private bool itemStartup;
    }

    public enum LibraryItemType
    {
        [Description("Importing..")]
        processing,
        [Description("Import complete.")]
        ready,
        cmdImport,
        multiImport,
        edit,
        gallery,
    }
}
