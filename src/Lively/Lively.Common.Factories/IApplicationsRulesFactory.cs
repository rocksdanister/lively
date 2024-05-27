using Lively.Common;
using Lively.Models;

namespace Lively.Common.Factories
{
    public interface IApplicationsRulesFactory
    {
        ApplicationRulesModel CreateAppPauseRule(string appPath, AppRulesEnum rule);

        AppMusicExclusionRuleModel CreateAppMusicExclusionRule(string appPath);
    }
}