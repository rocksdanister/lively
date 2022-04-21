using Lively.Common;

namespace Lively.Models
{
    public interface IApplicationRulesModel
    {
        string AppName { get; set; }
        AppRulesEnum Rule { get; set; }
    }
}