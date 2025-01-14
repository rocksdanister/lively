using Lively.Models;
using Lively.Models.Enums;
using System.Collections.ObjectModel;

namespace Lively.Common.Services
{
    public interface IResourceService
    {
        string GetString(string resource);
        string GetString(WallpaperType type);
        void SetCulture(string name);
        void SetSystemDefaultCulture();
    }
}
