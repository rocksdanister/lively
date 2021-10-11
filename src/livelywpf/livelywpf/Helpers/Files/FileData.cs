namespace livelywpf.Helpers.Files
{
    public class FileData
    {
        public WallpaperType Type { get; set; }
        public string[] Extentions { get; set; }
        public FileData(WallpaperType type, string[] extensions)
        {
            this.Type = type;
            this.Extentions = extensions;
        }
    }
}
