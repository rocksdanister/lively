namespace livelywpf.Models
{
    public interface IApplicationRulesModel
    {
        string AppName { get; set; }
        AppRulesEnum Rule { get; set; }
        string RuleText { get; set; }
    }
}