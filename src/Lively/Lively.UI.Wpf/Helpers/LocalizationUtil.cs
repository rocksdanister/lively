using Lively.Common;
using Lively.Common.Helpers.Files;
using Lively.Common.Helpers.Localization;
using Lively.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lively.UI.Wpf.Helpers
{
    public static class LocalizationUtil
    {
        public static List<LanguagesModel> SupportedLanguages = new List<LanguagesModel>()
        {
            new LanguagesModel("English(en)", new string[]{"en", "en-US"}), //default
            new LanguagesModel("日本語(ja)", new string[]{"ja", "ja-JP"}),
            new LanguagesModel("中文(zh-CN)", new string[] { "zh", "zh-Hans", "zh-CN", "zh-SG" }),
            new LanguagesModel("繁體中文(zh-Hant)", new string[] { "zh-HK", "zh-MO", "zh-TW" }),
            new LanguagesModel("한국어(ko-KR)", new string[] { "ko", "ko-KR", "ko-KP" }),
            new LanguagesModel("Pусский(ru)", new string[] { "ru", "ru-BY", "ru-KZ", "ru-KG", "ru-MD", "ru-RU", "ru-UA" }),
            new LanguagesModel("Українська(uk)", new string[] { "uk", "uk-UA" }),
            new LanguagesModel("Español(es)", new string[] { "es" }),
            new LanguagesModel("Español(es-MX)", new string[] { "es-MX" }),
            new LanguagesModel("Italian(it)", new string[] { "it", "it-IT", "it-SM", "it-CH", "it-VA" }),
            new LanguagesModel("عربى(ar-AE)", new string[] { "ar" }),
            new LanguagesModel("فارسی(fa-IR)", new string[] { "fa-IR" }),
            new LanguagesModel("עִברִית(he-IL)", new string[] { "he", "he-IL" }),
            new LanguagesModel("Française(fr)", new string[] { "fr" }),
            new LanguagesModel("Deutsch(de)", new string[] { "de" }),
            new LanguagesModel("język polski(pl)", new string[] { "pl", "pl-PL" }),
            new LanguagesModel("Português(pt)", new string[] { "pt" }),
            new LanguagesModel("Português(pt-BR)", new string[] { "pt-BR" }),
            new LanguagesModel("Filipino(fil)", new string[] { "fil", "fil-PH" }),
            new LanguagesModel("Bahasa Indonesia(id)", new string[] { "id", "id-ID" }),
            new LanguagesModel("Magyar(hu)", new string[] { "hu", "hu-HU" }),
            new LanguagesModel("Svenska(sv)", new string[] { "sv", "sv-AX", "sv-FI", "sv-SE" }),
            new LanguagesModel("Bahasa Melayu(ms)", new string[] { "ms", "ms-BN", "ms-MY" }),
            new LanguagesModel("Nederlands(nl-NL)", new string[] { "nl-NL" }),
            new LanguagesModel("Tiếng Việt(vi)", new string[] { "vi", "vi-VN" }),
            new LanguagesModel("Català(ca)", new string[] { "ca", "ca-AD", "ca-FR", "ca-IT", "ca-ES" }),
            new LanguagesModel("Türkçe(tr)", new string[] { "tr", "tr-CY", "tr-TR" }),
            new LanguagesModel("Cрпски језик(sr)", new string[] { "sr", "sr-Latn", "sr-Latn-BA", "sr-Latn-ME", "sr-Latn-RS", "sr-Latn-CS" }),
            new LanguagesModel("Српска ћирилица(sr-Cyrl)", new string[] { "sr-Cyrl", "sr-Cyrl-BA", "sr-Cyrl-ME", "sr-Cyrl-RS", "sr-Cyrl-CS" }),
            new LanguagesModel("Ελληνικά(el)", new string[] { "el", "el-GR", "el-CY" }),
            new LanguagesModel("हिन्दी(hi)", new string[] { "hi", "hi-IN" }),
            new LanguagesModel("Azerbaijani(az)", new string[] { "az", "az-Cyrl", "az-Cyrl-AZ" })
        };

        /// <summary>
        /// Returns language code if exists, default language(en) otherwise.
        /// </summary>
        /// <param name="langCode"></param>
        /// <returns></returns>
        public static LanguagesModel GetSupportedLanguage(string langCode) => 
            SupportedLanguages.FirstOrDefault(lang => lang.Codes.Contains(langCode)) ?? SupportedLanguages[0];

        public static string GetLocalizedWallpaperCategory(WallpaperType type)
        {
            return type switch
            {
                WallpaperType.app => Properties.Resources.TextApplication,
                WallpaperType.unity => "Unity",
                WallpaperType.godot => "Godot",
                WallpaperType.unityaudio => "Unity",
                WallpaperType.bizhawk => "Bizhawk",
                WallpaperType.web => Properties.Resources.TextWebsite,
                WallpaperType.webaudio => Properties.Resources.TitleAudio,
                WallpaperType.url => Properties.Resources.TextWebsite,
                WallpaperType.video => Properties.Resources.TextVideo,
                WallpaperType.gif => "Gif",
                WallpaperType.videostream => Properties.Resources.TextWebStream,
                WallpaperType.picture => Properties.Resources.TextPicture,
                //WallpaperType.heic => "HEIC",
                (WallpaperType)(100) => Properties.Resources.TitleAppName,
                _ => Properties.Resources.TextError,
            };
        }

        /// <summary>
        /// Generating filter text for file dialog (culture localised.)
        /// </summary>
        /// <param name="anyFile">Show any filetype.</param>
        /// <returns></returns>
        public static string GetLocalizedSupportedFileDialogFilter(bool anyFile = false)
        {
            StringBuilder filterString = new StringBuilder();
            if(anyFile)
            {
                filterString.Append(Properties.Resources.TextAllFiles + "|*.*|");
            }
            foreach (var item in FileFilter.LivelySupportedFormats)
            {
                filterString.Append(GetLocalizedWallpaperCategory(item.Type));
                filterString.Append("|");
                foreach (var extension in item.Extentions)
                {
                    filterString.Append("*").Append(extension).Append(";");
                }
                filterString.Remove(filterString.Length - 1, 1);
                filterString.Append("|");
            }
            filterString.Remove(filterString.Length - 1, 1);

            return filterString.ToString();
        }

        public static string GetLocalizedAppRules(AppRulesEnum rule)
        {
            return rule switch
            {
                AppRulesEnum.pause => Properties.Resources.TextPerformancePause,
                AppRulesEnum.ignore => Properties.Resources.TextPerformanceNone,
                AppRulesEnum.kill => Properties.Resources.TextPerformanceKill,
                _ => Properties.Resources.TextPerformanceNone,
            };
        }
    }
}
