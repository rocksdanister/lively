using System;

namespace Lively.Models
{
    public interface ILibraryModel
    {
        string Author { get; set; }
        LibraryItemType DataType { get; set; }
        string Desc { get; set; }
        string FilePath { get; set; }
        string ImagePath { get; set; }
        bool ItemStartup { get; set; }
        LivelyInfoModel LivelyInfo { get; set; }
        string LivelyInfoFolderPath { get; set; }
        string LivelyPropertyPath { get; set; }
        string PreviewClipPath { get; set; }
        Uri SrcWebsite { get; set; }
        string ThumbnailPath { get; set; }
        string Title { get; set; }
        public bool IsDownloading { get; set; }
        public float DownloadingProgress { get; set; }
        public string DownloadingProgressText { get; set; }
        public bool IsSubscribed { get; set; }
    }
}