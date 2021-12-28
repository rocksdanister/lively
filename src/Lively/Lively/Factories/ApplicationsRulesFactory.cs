using Lively.Common;
using Lively.Models;
using System.IO;

namespace Lively.Factories
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
