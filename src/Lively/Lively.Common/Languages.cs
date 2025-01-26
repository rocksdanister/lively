using Lively.Models;
using System;
using System.Collections.ObjectModel;
using System.Linq;

namespace Lively.Common
{
    public static class Languages
    {
        private readonly static LanguageModel[] supportedLanguages = {
            new LanguageModel("English", "en-US"),
            new LanguageModel("日本語", "ja-JP"), // Japanese
            new LanguageModel("中文", "zh-CN"), // Chinese (Simplified)
            new LanguageModel("中文 (繁體)", "zh-Hant"), // Chinese (Traditional)
            new LanguageModel("한국어", "ko-KR"), // Korean
            new LanguageModel("Pусский", "ru-RU"), // Russian
            new LanguageModel("Українська", "uk-UA"), // Ukrainian
            new LanguageModel("Español", "es-ES"), // Spanish
            new LanguageModel("Español (México)", "es-MX"), // Spanish (Mexico)
            new LanguageModel("Italiano", "it-IT"), // Italian
            new LanguageModel("عربى", "ar-AE"), // Arabic (United Arab Emirates)
            new LanguageModel("فارسی", "fa-IR"), // Persian
            new LanguageModel("עִברִית", "he-IL"), // Hebrew
            new LanguageModel("Française", "fr-FR"), // French
            new LanguageModel("Deutsch", "de-DE"), // German
            new LanguageModel("Polski", "pl-PL"), // Polish
            new LanguageModel("Português", "pt-PT"),// Portuguese (Portugal)
            new LanguageModel("Português", "pt-BR"), // Portuguese (Brazil)
            new LanguageModel("Filipino", "fil-PH"), // Filipino
            new LanguageModel("Finnish", "fi-FI"), // Finnish
            new LanguageModel("Bahasa Indonesia", "id-ID"), // Indonesian
            new LanguageModel("Magyar", "hu-HU"), // Hungarian
            new LanguageModel("Svenska", "sv-SE"), // Swedish
            new LanguageModel("Bahasa Melayu", "ms-MY"), // Malay
            new LanguageModel("Nederlands", "nl-NL"), // Dutch
            new LanguageModel("Tiếng Việt", "vi-VN"), // Vietnamese
            new LanguageModel("Català", "ca-ES"), // Catalan
            new LanguageModel("Türkçe", "tr-TR"), // Turkish
            new LanguageModel("Cрпски језик", "sr-Latn"), // Serbian (Latin)
            new LanguageModel("Српска ћирилица", "sr-Cyrl"), //Serbian (Cyrillic)
            new LanguageModel("Ελληνικά", "el-GR"), // Greek
            new LanguageModel("हिन्दी", "hi-IN"), // Hindi
            new LanguageModel("Azərbaycan", "az-Latn"), // Azerbaijani (Latin)
            new LanguageModel("Čeština", "cs-CZ"), // Czech
            new LanguageModel("Български", "bg-BG"), // Bulgarian
            new LanguageModel("Norwegian Bokmål", "nb-NO"), //Norwegian
            new LanguageModel("lietuvių kalba", "lt-LT"), //Lithuanian
            new LanguageModel("Afrikaans", "af-ZA"), // Afrikaans
            new LanguageModel("Dansk", "da-DK"), // Danish
            new LanguageModel("беларуская мова", "be-BY"), // Belarusian
            new LanguageModel("galego", "gl-ES"), // Galician (Spain)
            new LanguageModel("қазақ тілі", "kk-KZ"), // Kazakh (Kazakhstan)
            new LanguageModel("မြန်မာဘာသာ", "my-MM"), // Burmese
            new LanguageModel("slovenčina", "sk-SK"), // Slovak (Slovakia)
            new LanguageModel("Gaelic", "ga-IE"), // Irish
            new LanguageModel("română", "ro-RO"), // Romanian
        };

        public static ReadOnlyCollection<LanguageModel> SupportedLanguages => Array.AsReadOnly(supportedLanguages);
    }
}
