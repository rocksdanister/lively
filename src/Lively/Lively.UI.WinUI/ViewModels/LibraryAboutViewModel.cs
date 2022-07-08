using Lively.Common;
using Lively.Common.Helpers.Files;
using Lively.Models;
using System;

namespace Lively.UI.WinUI.ViewModels
{
    internal class LibraryAboutViewModel
    {
        public LibraryAboutViewModel(ILibraryModel obj)
        {
            Title = obj.Title;
            Desc = obj.Desc;
            Author = obj.Author;
            SrcWebsite = obj.SrcWebsite;
            Type = obj.LivelyInfo.Type;
            Contact = obj.LivelyInfo.Contact;
            IsInstalled = !obj.LivelyInfo.IsAbsolutePath;

            try
            {
                DirectorySize = FileOperations.SizeSuffix(FileOperations.GetDirectorySize(obj.LivelyInfoFolderPath), 2);
            }
            catch { }
        }

        public string Title { get; }
        public string Desc { get; }
        public string Author { get; }
        public Uri SrcWebsite { get; }
        public WallpaperType Type { get; }
        public string Contact { get; }
        public bool IsInstalled { get; }
        public string DirectorySize { get; } = "--";
    }
}
