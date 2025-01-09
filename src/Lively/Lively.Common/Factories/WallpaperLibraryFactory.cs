using Lively.Common.Extensions;
using Lively.Common.Helpers.Storage;
using Lively.Models;
using System.IO;

namespace Lively.Common.Factories
{
    public class WallpaperLibraryFactory : IWallpaperLibraryFactory
    {
        public LivelyInfoModel GetMetadata(string folderPath)
        {
            if (!File.Exists(Path.Combine(folderPath, "LivelyInfo.json")))
                throw new FileNotFoundException("LivelyInfo.json not found");

            return JsonStorage<LivelyInfoModel>.LoadData(Path.Combine(folderPath, "LivelyInfo.json")) ?? throw new FileNotFoundException("Corrupted wallpaper metadata");
        }

        public LibraryModel CreateFromDirectory(string folderPath)
        {
            var metadata = GetMetadata(folderPath);
            var result = new LibraryModel
            {
                LivelyInfo = metadata,
                LivelyInfoFolderPath = folderPath,
                IsSubscribed = !string.IsNullOrEmpty(metadata.Id),
                DataType = LibraryItemType.ready,
                Title = metadata.Title,
                Desc = metadata.Desc,
                Author = metadata.Author
            };
            if (metadata.IsAbsolutePath)
            {
                //Full filepath is stored in Livelyinfo.json metadata file.
                result.FilePath = metadata.FileName;
                //This is to keep backward compatibility with older wallpaper files.
                //When I originally made the property all the paths where made absolute, not just wallpaper path.
                //But previewgif and thumb are always inside the temporary lively created folder.
                result.PreviewClipPath = TryPathCombine(folderPath, Path.GetFileName(metadata.Preview));
                result.ThumbnailPath = TryPathCombine(folderPath, Path.GetFileName(metadata.Thumbnail));

                try
                {
                    result.LivelyPropertyPath = Path.Combine(Directory.GetParent(metadata.FileName).FullName, "LivelyProperties.json");
                }
                catch
                {
                    result.LivelyPropertyPath = null;
                }
            }
            else
            {
                //Only relative path is stored, this will be inside the appdata folder
                if (metadata.Type.IsOnlineWallpaper())
                    result.FilePath = metadata.FileName;
                else
                {
                    result.FilePath = TryPathCombine(folderPath, metadata.FileName);
                    result.LivelyPropertyPath = TryPathCombine(folderPath, "LivelyProperties.json");
                }
                result.PreviewClipPath = TryPathCombine(folderPath, metadata.Preview);
                result.ThumbnailPath = TryPathCombine(folderPath, metadata.Thumbnail);
            }

            //Use preview if available
            result.ImagePath = File.Exists(result.PreviewClipPath) ? result.PreviewClipPath : result.ThumbnailPath;
            //Default video player property, otherwise verify if wallpaper is customisable
            if (metadata.Type.IsMediaWallpaper())
                result.LivelyPropertyPath = File.Exists(result.LivelyPropertyPath) ? 
                    result.LivelyPropertyPath : Path.Combine(Constants.CommonPaths.TempVideoDir, "LivelyProperties.json");
            else
                result.LivelyPropertyPath = File.Exists(result.LivelyPropertyPath) ? result.LivelyPropertyPath : null;

            return result;
        }

        private static string TryPathCombine(string path1, string path2)
        {
            try
            {
                return Path.Combine(path1, path2);
            }
            catch
            {
                return null;
            }
        }

        public LibraryModel CreateFromMetadata(LivelyInfoModel metadata)
        {
            return new LibraryModel()
            {
                LivelyInfo = metadata,
                Title = metadata.Title,
                Desc = metadata.Desc,
                Author = metadata.Author,
                ImagePath = metadata.Preview ?? metadata.Thumbnail,
            };
        }
    }
}