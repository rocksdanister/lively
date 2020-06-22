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

        //lang-codes: https://docs.microsoft.com/en-us/openspecs/windows_protocols/ms-lcid/a9eac961-e77d-41a6-90a5-ce1a8b0cdb9c
        public static readonly LanguagesModel[] SupportedLanguages = new LanguagesModel[] {
                                    new LanguagesModel("English(en-US)", new string[]{"en-US"}), //technically not US english, sue me..
                                    new LanguagesModel("中文(zh-CN)", new string[]{"zh", "zh-Hans","zh-CN","zh-SG"}), //are they same?
                                    new LanguagesModel("日本人(ja-JP)", new string[]{"ja", "ja-JP"}),
                                    new LanguagesModel("Pусский(ru)", new string[]{"ru", "ru-BY", "ru-KZ", "ru-KG", "ru-MD", "ru-RU","ru-UA"}), //are they same?
                                    new LanguagesModel("हिन्दी(hi-IN)", new string[]{"hi", "hi-IN"}),
                                    new LanguagesModel("Español(es)", new string[]{"es"}),
                                    new LanguagesModel("Italian(it)", new string[]{"it", "it-IT", "it-SM","it-CH","it-VA"}),
                                    new LanguagesModel("عربى(ar-AE)", new string[]{"ar"}),
                                    new LanguagesModel("Française(fr)", new string[]{"fr"}),
                                    new LanguagesModel("Deutsche(de)", new string[]{"de"}),
                                    new LanguagesModel("portuguesa(pt)", new string[]{"pt"}),
                                    };
    }
}
