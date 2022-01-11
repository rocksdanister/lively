using Lively.Common;
using Lively.Models;

namespace Lively.UI.Wpf.Factories
{
    public interface IApplicationsRulesFactory
    {
        IApplicationRulesModel CreateAppRule(string appPath, AppRulesEnum rule);
    }
}