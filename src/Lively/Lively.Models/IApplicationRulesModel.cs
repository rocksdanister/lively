using Lively.Common;

namespace Lively.Models
{
    public interface IApplicationRulesModel
    {
        string AppName { get; set; }
        AppRulesEnum Rule { get; set; }
        /// <summary>
        /// For localization use only.
        /// </summary>
        string RuleText { get; set; }
    }
}