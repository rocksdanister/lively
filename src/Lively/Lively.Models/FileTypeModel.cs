using Lively.Models.Enums;

namespace Lively.Models
{
    public class FileTypeModel
    {
        public WallpaperType Type { get; set; }
        public string[] Extentions { get; set; }

        public FileTypeModel(WallpaperType type, string[] extensions)
        {
            Type = type;
            Extentions = extensions;
        }
    }
}
