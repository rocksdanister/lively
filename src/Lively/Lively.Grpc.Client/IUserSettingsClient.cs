using Lively.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Lively.Grpc.Client
{
    public interface IUserSettingsClient
    {
        SettingsModel Settings { get; }
        List<ApplicationRulesModel> AppRules { get; }
        Task SaveAsync<T>();
        void Save<T>();
        Task LoadAsync<T>();
        void Load<T>();
    }
}