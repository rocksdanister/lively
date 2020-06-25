using System;
using System.Collections.Generic;
using System.Text;

namespace livelywpf
{
    [Serializable]
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
