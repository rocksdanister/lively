using Lively.Models.Enums;
using System.Collections.Generic;

namespace Lively.Models.Gallery.API
{
    public partial class WallpaperDto
    {
        public string Id { get; set; }
        public string AppVersion { get; set; }
        public string Title { get; set; }
        public ProfileDto Author { get; set; }
        public string License { get; set; }
        public string Contact { get; set; }
        public bool IsPreviewAvailable { get; set; }
        //Assigned in GalleryClient.cs:SearchWallpapersAsync()
        public string Thumbnail { get; set; }
        public string Preview { get; set; }
        public WallpaperType Type { get; set; }
        public int VoteCount { get; set; }
        public string Description { get; set; }
        public List<string> Tags { get; set; }
    }
}
