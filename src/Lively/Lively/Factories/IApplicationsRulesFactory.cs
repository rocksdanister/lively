using Lively.Common;
using Lively.Models;

namespace Lively.Factories
{
    public interface IApplicationsRulesFactory
    {
        IApplicationRulesModel CreateAppRule(string appPath, AppRulesEnum rule);
    }
}