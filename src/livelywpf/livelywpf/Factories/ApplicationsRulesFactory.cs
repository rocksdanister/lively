using livelywpf.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace livelywpf.Factories
{
    public class ApplicationsRulesFactory : IApplicationsRulesFactory
    {
        public IApplicationRulesModel CreateAppRule(string appPath, AppRulesEnum rule)
        {
            var fileName = Path.GetFileNameWithoutExtension(appPath);
            return new ApplicationRulesModel(fileName, rule);
        }
    }
}
