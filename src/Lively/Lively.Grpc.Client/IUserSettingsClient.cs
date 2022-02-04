using Lively.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Lively.Grpc.Client
{
    public interface IUserSettingsClient
    {
        ISettingsModel Settings { get; }
        List<IApplicationRulesModel> AppRules { get; }
        Task SaveAsync<T>();
        void Save<T>();
        Task LoadAsync<T>();
        void Load<T>();
    }
}