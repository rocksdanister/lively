using Lively.Common;
using Lively.Common.Helpers.Files;
using Lively.Models;
using Lively.Models.Enums;
using System;

namespace Lively.UI.Shared.ViewModels
{
    internal class LibraryAboutViewModel
    {
        public LibraryAboutViewModel(LibraryModel obj)
        {
            Title = obj.Title;
            Desc = obj.Desc;
            Author = obj.Author;
            SrcWebsite = LinkUtil.TrySanitizeUrl(obj.LivelyInfo.Contact, out Uri uri) ? uri : null;
            Type = obj.LivelyInfo.Type;
            Contact = obj.LivelyInfo.Contact;
            IsInstalled = !obj.LivelyInfo.IsAbsolutePath;

            try
            {
                DirectorySize = FileUtil.SizeSuffix(FileUtil.GetDirectorySize(obj.LivelyInfoFolderPath), 2);
            }
            catch (Exception ex)
            {
                DirectorySize = ex.Message;
            }
        }

        public string Title { get; }
        public string Desc { get; }
        public string Author { get; }
        public Uri SrcWebsite { get; }
        public WallpaperType Type { get; }
        public string Contact { get; }
        public bool IsInstalled { get; }
        public string DirectorySize { get; }
    }
}
