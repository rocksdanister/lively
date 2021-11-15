using livelywpf.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace livelywpf.Factories
{
    public class ApplicationsRulesFactory : IApplicationsRulesFactory
    {
        public IApplicationRulesModel CreateAppRule(string appName, AppRulesEnum rule)
        {
            return new ApplicationRulesModel(appName, rule);
        }
    }
}
