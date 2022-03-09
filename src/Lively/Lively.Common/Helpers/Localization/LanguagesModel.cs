using System;

namespace Lively.Common.Helpers.Localization
{
    [Serializable]
    public class LanguagesModel : ILanguagesModel
    {
        public string Language { get; set; }
        public string[] Codes { get; set; }

        public LanguagesModel(string language, string[] codes)
        {
            this.Language = language;
            this.Codes = codes;
        }
    }
}
