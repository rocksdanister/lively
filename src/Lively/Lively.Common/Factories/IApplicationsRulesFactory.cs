using Lively.Models;
using Lively.Models.Enums;

namespace Lively.Common.Factories
{
    public interface IApplicationsRulesFactory
    {
        ApplicationRulesModel CreateAppPauseRule(string appPath, AppRules rule);

        AppMusicExclusionRuleModel CreateAppMusicExclusionRule(string appPath);
    }
}