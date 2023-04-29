using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using Lively.Common.Helpers.Localization;

namespace Lively.Common
{
    public static class SupportedLanguages
    {
        private readonly static LanguagesModel[] languages = {
            new LanguagesModel("English(en)", new string[]{"en", "en-US"}), //default
            new LanguagesModel("日本語(ja)", new string[]{"ja", "ja-JP"}),
            new LanguagesModel("中文(zh-CN)", new string[] { "zh", "zh-Hans", "zh-CN", "zh-SG" }),
            new LanguagesModel("繁體中文(zh-Hant)", new string[] { "zh-HK", "zh-MO", "zh-TW" }),
            new LanguagesModel("한국어(ko-KR)", new string[] { "ko", "ko-KR", "ko-KP" }),
            new LanguagesModel("Pусский(ru)", new string[] { "ru", "ru-BY", "ru-KZ", "ru-KG", "ru-MD", "ru-RU", "ru-UA" }),
            new LanguagesModel("Українська(uk)", new string[] { "uk", "uk-UA" }),
            new LanguagesModel("Español(es)", new string[] { "es", "es-ES" }),
            new LanguagesModel("Español(es-MX)", new string[] { "es-MX" }),
            new LanguagesModel("Italian(it)", new string[] { "it", "it-IT", "it-SM", "it-CH", "it-VA" }),
            new LanguagesModel("عربى(ar-AE)", new string[] { "ar", "ar-AE" }),
            new LanguagesModel("فارسی(fa-IR)", new string[] { "fa", "fa-IR" }),
            new LanguagesModel("עִברִית(he-IL)", new string[] { "he", "he-IL" }),
            new LanguagesModel("Française(fr)", new string[] { "fr", "fr-FR" }),
            new LanguagesModel("Deutsch(de)", new string[] { "de", "de-DE" }),
            new LanguagesModel("język polski(pl)", new string[] { "pl", "pl-PL" }),
            new LanguagesModel("Português(pt)", new string[] { "pt", "pt-PT" }),
            new LanguagesModel("Português(pt-BR)", new string[] { "pt-BR" }),
            new LanguagesModel("Filipino(fil)", new string[] { "fil", "fil-PH" }),
            new LanguagesModel("Finnish(fi)", new string[] { "fi", "fi-FI" }),
            new LanguagesModel("Bahasa Indonesia(id)", new string[] { "id", "id-ID" }),
            new LanguagesModel("Magyar(hu)", new string[] { "hu", "hu-HU" }),
            new LanguagesModel("Svenska(sv)", new string[] { "sv", "sv-AX", "sv-FI", "sv-SE" }),
            new LanguagesModel("Bahasa Melayu(ms)", new string[] { "ms", "ms-BN", "ms-MY" }),
            new LanguagesModel("Nederlands(nl-NL)", new string[] { "nl", "nl-NL" }),
            new LanguagesModel("Tiếng Việt(vi)", new string[] { "vi", "vi-VN" }),
            new LanguagesModel("Català(ca)", new string[] { "ca", "ca-AD", "ca-FR", "ca-IT", "ca-ES" }),
            new LanguagesModel("Türkçe(tr)", new string[] { "tr", "tr-CY", "tr-TR" }),
            new LanguagesModel("Cрпски језик(sr)", new string[] { "sr", "sr-Latn", "sr-Latn-BA", "sr-Latn-ME", "sr-Latn-RS", "sr-Latn-CS" }),
            new LanguagesModel("Српска ћирилица(sr-Cyrl)", new string[] { "sr-Cyrl", "sr-Cyrl-BA", "sr-Cyrl-ME", "sr-Cyrl-RS", "sr-Cyrl-CS" }),
            new LanguagesModel("Ελληνικά(el)", new string[] { "el", "el-GR", "el-CY" }),
            new LanguagesModel("हिन्दी(hi)", new string[] { "hi", "hi-IN" }),
            new LanguagesModel("Azərbaycan(az)", new string[] { "az", "az-Cyrl", "az-Cyrl-AZ" }),
            new LanguagesModel("Čeština(cs)", new string[] { "cs", "cs-CZ" }),
            new LanguagesModel("български(bg)", new string[] { "bg", "bg-BG" }),
            new LanguagesModel("Norwegian Bokmål(nb)", new string[] { "nb", "nb-NO" }),
            new LanguagesModel("lietuvių kalba(lt)", new string[] { "lt", "lt-LT" }),
            new LanguagesModel("Afrikaans(af)", new string[] { "af", "af-ZA" }),
            new LanguagesModel("Dansk(da)", new string[] { "da", "da-DK" }),
        };

        public static ReadOnlyCollection<LanguagesModel> Languages => Array.AsReadOnly(languages);

        /// <summary>
        /// Returns language code if exists, default language(en) otherwise.
        /// </summary>
        /// <param name="langCode"></param>
        /// <returns></returns>
        public static LanguagesModel GetLanguage(string langCode) =>
            Languages.FirstOrDefault(lang => lang.Codes.Contains(langCode, StringComparer.OrdinalIgnoreCase)) ?? Languages[0];
    }
}
