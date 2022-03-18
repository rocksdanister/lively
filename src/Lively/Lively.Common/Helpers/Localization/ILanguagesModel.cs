namespace Lively.Common.Helpers.Localization
{
    public interface ILanguagesModel
    {
        string[] Codes { get; set; }
        string Language { get; set; }
    }
}