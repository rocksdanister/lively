using System;

namespace Lively.Common.Helpers.Localization
{
    public class LanguagesModel
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
