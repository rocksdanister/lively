using Lively.Models.Enums;
using System;

namespace Lively.Common.Services
{
    public interface IResourceService
    {
        event EventHandler<string> CultureChanged;

        string GetString(string resource);
        string GetString(WallpaperType type);
        void SetCulture(string name);
        void SetSystemDefaultCulture();
    }
}
