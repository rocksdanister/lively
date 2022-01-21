using Lively.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Lively.Grpc.Client
{
    public interface IUserSettingsClient
    {
        ISettingsModel Settings { get; }
        List<IApplicationRulesModel> AppRules { get; }
        //List<IWallpaperLayoutModel> WallpaperLayout { get; }
        Task Save<T>();
        Task Load<T>();
    }
}