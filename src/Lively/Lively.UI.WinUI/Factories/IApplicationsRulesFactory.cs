using Lively.Common;
using Lively.Models;

namespace Lively.UI.WinUI.Factories
{
    public interface IApplicationsRulesFactory
    {
        ApplicationRulesModel CreateAppRule(string appPath, AppRulesEnum rule);
    }
}