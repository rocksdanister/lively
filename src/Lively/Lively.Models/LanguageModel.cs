namespace Lively.Models
{
    public class LanguageModel
    {
        public string DisplayName { get; set; }
        public string Code { get; set; }

        public LanguageModel(string displayName, string code)
        {
            DisplayName = displayName;
            Code = code;
        }
    }
}
