namespace Lively.Models
{
    public class LanguagesModel
    {
        public string Language { get; set; }
        public string[] Codes { get; set; }

        public LanguagesModel(string language, string[] codes)
        {
            Language = language;
            Codes = codes;
        }
    }
}
