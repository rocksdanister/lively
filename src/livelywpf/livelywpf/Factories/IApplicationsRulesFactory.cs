using livelywpf.Models;

namespace livelywpf.Factories
{
    public interface IApplicationsRulesFactory
    {
        IApplicationRulesModel CreateAppRule(string appPath, AppRulesEnum rule);
    }
}