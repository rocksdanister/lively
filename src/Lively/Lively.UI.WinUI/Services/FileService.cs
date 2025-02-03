using Lively.Common;
using Lively.Common.Services;
using Lively.Models.Enums;
using Lively.UI.WinUI.Extensions;
using Lively.UI.WinUI.Helpers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml.Controls;
using NLog.Filters;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage.Pickers;
using UAC = UACHelper.UACHelper;

namespace Lively.UI.WinUI.Services
{
    //References:
    //https://github.com/microsoft/WindowsAppSDK/issues/2504
    //https://learn.microsoft.com/en-us/previous-versions/dotnet/netframework-4.0/w5tyztk9(v=vs.100)
    //https://gist.github.com/gotmachine/4ffaf7837f9fbb0ab4a648979ee40609
    //https://learn.microsoft.com/en-us/windows/win32/api/commdlg/ns-commdlg-openfilenamea
    public class FileService : IFileService
    {
        private readonly IResourceService i18n;

        public FileService(IResourceService i18n)
        {
            this.i18n = i18n;
        }

        public async Task<IReadOnlyList<string>> PickFileAsync(string[] filters, bool multipleFile = false)
        {
            if (!multipleFile)
            {
                //var file = UAC.IsElevated ?
                //    PickSingleFileNative(filters) :
                //    await PickSingleFileUwp(filters);
                var file = await PickSingleFileUwp(filters);
                return file != null ? [file] : [];
            }
            else
            {
                //return UAC.IsElevated ?
                //  PickMultipleFileNative(filters) :
                //  await PickMultipleFileUwp(filters);
                return await PickMultipleFileUwp(filters);
            }
        }

        public async Task<IReadOnlyList<string>> PickFileAsync(WallpaperType type, bool multipleFile = false)
        {
            if (!multipleFile)
            {
                var file = UAC.IsElevated ?
                    PickSingleFileNative(FileDialogFilterNative(type)) :
                    await PickSingleFileUwp(FileDialogFilter(type));
                return file != null ? [file] : [];
            }
            else
            {
                return UAC.IsElevated ?
                  PickMultipleFileNative(FileDialogFilterNative(type)) :
                  await PickMultipleFileUwp(FileDialogFilter(type));
            }
        }

        public async Task<IReadOnlyList<string>> PickWallpaperFile(bool multipleFile = false)
        {
            if (!multipleFile)
            {
                var file = UAC.IsElevated ?
                    PickSingleFileNative(FileDialogFilterAllNative(true)) :
                    await PickSingleFileUwp(FileDialogFilterAll(true).ToArray());
                return file != null ? [file] : [];
            }
            else
            {
                return UAC.IsElevated ?
                  PickMultipleFileNative(FileDialogFilterAllNative(true)) :
                  await PickMultipleFileUwp(FileDialogFilterAll(true).ToArray());
            }
        }

        public async Task<string> PickFolderAsync(string[] filters)
        {
            var folderPicker = new FolderPicker();
            folderPicker.SetOwnerWindow(App.Services.GetRequiredService<MainWindow>());
            foreach (var item in filters)
            {
                folderPicker.FileTypeFilter.Add(item);
            }
            return (await folderPicker.PickSingleFolderAsync())?.Path;
        }

        public async Task<string> PickSaveFileAsync(string suggestedFileName, IDictionary<string, IList<string>> fileTypeChoices)
        {
            var filePicker = new FileSavePicker();
            filePicker.SetOwnerWindow(App.Services.GetRequiredService<MainWindow>());
            foreach (var item in fileTypeChoices)
                filePicker.FileTypeChoices.Add(item.Key, item.Value);
            filePicker.SuggestedFileName = suggestedFileName;
            var file = await filePicker.PickSaveFileAsync();
            return file?.Path;
        }

        private static async Task<string> PickSingleFileUwp(string[] filters)
        {
            var filePicker = new FileOpenPicker();
            filePicker.SetOwnerWindow(App.Services.GetRequiredService<MainWindow>());
            foreach (var item in filters)
            {
                filePicker.FileTypeFilter.Add(item);
            }
            return (await filePicker.PickSingleFileAsync())?.Path;
        }

        private static async Task<IReadOnlyList<string>> PickMultipleFileUwp(string[] filters)
        {
            var filePicker = new FileOpenPicker();
            foreach (var item in filters)
            {
                filePicker.FileTypeFilter.Add(item);
            }
            filePicker.SetOwnerWindow(App.Services.GetRequiredService<MainWindow>());
            var files = await filePicker.PickMultipleFilesAsync();
            return files.Any() ? files.Select(x => x.Path).ToList() : new List<string>();
        }

        private static string PickSingleFileNative(string filter)
        {
            var files = ShowOpenFileDialog(filter);
            return files.Any() ? files[0] : null;
        }

        private static IReadOnlyList<string> PickMultipleFileNative(string filter)
        {
            return ShowOpenFileDialog(filter, true);
        }

        private static List<string> FileDialogFilterAll(bool anyFile = false)
        {
            var filterCollection = new List<string>();
            if (anyFile)
            {
                filterCollection.Add("*");
            }
            foreach (var item in FileTypes.SupportedFormats)
            {
                foreach (var extension in item.Extentions)
                {
                    filterCollection.Add(extension);
                }
            }
            return filterCollection.Distinct().ToList();
        }

        private string[] FileDialogFilter(WallpaperType wallpaperType) =>
            FileTypes.SupportedFormats.First(x => x.Type == wallpaperType).Extentions;

        private string FileDialogFilterNative(WallpaperType wallpaperType)
        {
            var filterString = new StringBuilder();
            var selection = FileTypes.SupportedFormats.First(x => x.Type == wallpaperType);
            filterString.Append(i18n.GetString(selection.Type)).Append('\0');
            foreach (var extension in selection.Extentions)
            {
                filterString.Append('*').Append(extension).Append(';');
            }
            filterString.Remove(filterString.Length - 1, 1).Append('\0');
            filterString.Remove(filterString.Length - 1, 1).Append('\0');
            return filterString.ToString();
        }

        private string FileDialogFilterAllNative(bool anyFile = false)
        {
            var filterString = new StringBuilder();
            if (anyFile)
            {
                filterString.Append(i18n.GetString("TextAllFiles")).Append("\0*.*\0");
            }
            foreach (var item in FileTypes.SupportedFormats)
            {
                filterString.Append(i18n.GetString(item.Type)).Append('\0');
                foreach (var extension in item.Extentions)
                {
                    filterString.Append('*').Append(extension).Append(';');
                }
                filterString.Remove(filterString.Length - 1, 1).Append('\0');
            }
            filterString.Remove(filterString.Length - 1, 1).Append('\0');
            return filterString.ToString();
        }

        #region openfiledialog pinvoke

        [DllImport("comdlg32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern bool GetOpenFileName([In, Out] OpenFileName ofn);

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private class OpenFileName
        {
            public int structSize = 0;
            public IntPtr dlgOwner = IntPtr.Zero;
            public IntPtr instance = IntPtr.Zero;
            public string filter;
            public string customFilter;
            public int maxCustFilter = 0;
            public int filterIndex = 0;
            public IntPtr file;
            public int maxFile = 0;
            public string fileTitle;
            public int maxFileTitle = 0;
            public string initialDir;
            public string title;
            public int flags = 0;
            public short fileOffset = 0;
            public short fileExtension = 0;
            public string defExt;
            public IntPtr custData = IntPtr.Zero;
            public IntPtr hook = IntPtr.Zero;
            public string templateName;
            public IntPtr reservedPtr = IntPtr.Zero;
            public int reservedInt = 0;
            public int flagsEx = 0;
        }

        private enum OpenFileNameFlags
        {
            OFN_HIDEREADONLY = 0x4,
            OFN_FORCESHOWHIDDEN = 0x10000000,
            OFN_ALLOWMULTISELECT = 0x200,
            OFN_EXPLORER = 0x80000,
            OFN_FILEMUSTEXIST = 0x1000,
            OFN_PATHMUSTEXIST = 0x800
        }

        private static IReadOnlyList<string> ShowOpenFileDialog(string filter, bool multiSelect = false)
        {
            const int MAX_FILE_LENGTH = 2048;
            var ofn = new OpenFileName();
            ofn.structSize = Marshal.SizeOf(ofn);
            ofn.filter = filter;//filter?.Replace("|", "\0") + "\0";
            ofn.fileTitle = new string(new char[MAX_FILE_LENGTH]);
            ofn.maxFileTitle = ofn.fileTitle.Length;
            ofn.flags = (int)OpenFileNameFlags.OFN_HIDEREADONLY | (int)OpenFileNameFlags.OFN_EXPLORER | (int)OpenFileNameFlags.OFN_FILEMUSTEXIST | (int)OpenFileNameFlags.OFN_PATHMUSTEXIST;

            // Create buffer for file names
            ofn.file = Marshal.AllocHGlobal(MAX_FILE_LENGTH * Marshal.SystemDefaultCharSize);
            ofn.maxFile = MAX_FILE_LENGTH;

            // Initialize buffer with NULL bytes
            for (int i = 0; i < MAX_FILE_LENGTH * Marshal.SystemDefaultCharSize; i++)
            {
                Marshal.WriteByte(ofn.file, i, 0);
            }

            if (multiSelect)
            {
                //If the user selects more than one file, the lpstrFile buffer returns the path to the current directory followed by the file names of the selected files.
                //The nFileOffset member is the offset, in bytes or characters, to the first file name, and the nFileExtension member is not used.
                //For Explorer-style dialog boxes, the directory and file name strings are NULL separated, with an extra NULL character after the last file name.
                //This format enables the Explorer-style dialog boxes to return long file names that include spaces.
                ofn.flags |= (int)OpenFileNameFlags.OFN_ALLOWMULTISELECT;
            }

            var result = new List<string>();
            var success = GetOpenFileName(ofn);
            if (success)
            {
                IntPtr filePointer = ofn.file;
                long pointer = (long)filePointer;
                string file = Marshal.PtrToStringAuto(filePointer);
                var strList = new List<string>();

                // Retrieve file names
                while (file.Length > 0)
                {
                    strList.Add(file);

                    pointer += file.Length * Marshal.SystemDefaultCharSize + Marshal.SystemDefaultCharSize;
                    filePointer = checked((IntPtr)pointer);
                    file = Marshal.PtrToStringAuto(filePointer);
                }

                if (strList.Count > 1)
                {
                    for (int i = 1; i < strList.Count; i++)
                    {
                        result.Add(Path.Combine(strList[0], strList[i]));
                    }
                }
                else
                {
                    result.AddRange(strList);
                }
            }
            Marshal.FreeHGlobal(ofn.file);

            return result;
        }

        #endregion //openfiledialog pinvoke
    }
}
