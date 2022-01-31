using Lively.Common;
using Lively.Models;

namespace Lively.UI.WinUI.Factories
{
    public interface IApplicationsRulesFactory
    {
        IApplicationRulesModel CreateAppRule(string appPath, AppRulesEnum rule);
    }
}