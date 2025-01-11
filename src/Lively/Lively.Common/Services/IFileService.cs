using Lively.Models.Enums;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Lively.Common.Services
{
    public interface IFileService
    {
        public Task<IReadOnlyList<string>> PickFileAsync(string[] filters, bool multipleFile = false);

        public Task<IReadOnlyList<string>> PickFileAsync(WallpaperType type, bool multipleFile = false);

        public Task<IReadOnlyList<string>> PickWallpaperFile(bool multipleFile = false);

        public Task<string> PickFolderAsync(string[] filters);

        public Task<string> PickSaveFileAsync(string suggestedFileName, IDictionary<string, IList<string>> fileTypeChoices);
    }
}
