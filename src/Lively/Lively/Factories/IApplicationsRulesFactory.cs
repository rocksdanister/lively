using Lively.Common;
using Lively.Models;

namespace Lively.Factories
{
    public interface IApplicationsRulesFactory
    {
        ApplicationRulesModel CreateAppRule(string appPath, AppRulesEnum rule);
    }
}