using Lively.Common;
using Lively.Common.Helpers.Files;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage.Pickers;
using UAC = UACHelper.UACHelper;

namespace Lively.UI.WinUI.Helpers
{

    //References:
    //https://github.com/microsoft/WindowsAppSDK/issues/2504
    //https://learn.microsoft.com/en-us/previous-versions/dotnet/netframework-4.0/w5tyztk9(v=vs.100)
    //https://gist.github.com/gotmachine/4ffaf7837f9fbb0ab4a648979ee40609
    //https://learn.microsoft.com/en-us/windows/win32/api/commdlg/ns-commdlg-openfilenamea
    public static class FilePickerUtil
    {
        public static async Task<string> PickSingleFile(WallpaperType type)
        {
            return UAC.IsElevated ? 
                PickSingleFileNative(LocalizationUtil.FileDialogFilterNative(type)) : 
                await PickSingleFileUwp(LocalizationUtil.FileDialogFilter(type));
        }

        public static async Task<IReadOnlyList<string>> PickMultipleFile(WallpaperType type)
        {
            return UAC.IsElevated ?
                PickMultipleFileNative(LocalizationUtil.FileDialogFilterNative(type)) :
                await PickMultipleFileUwp(LocalizationUtil.FileDialogFilter(type));
        }

        public static async Task<string> PickLivelyWallpaperSingleFile()
        {
            return UAC.IsElevated ?
                PickSingleFileNative(LocalizationUtil.FileDialogFilterAllNative(true)) :
                await PickSingleFileUwp(LocalizationUtil.FileDialogFilterAll(true).ToArray());
        }

        public static async Task<IReadOnlyList<string>> PickLivelyWallpaperMultipleFile()
        {
            return UAC.IsElevated ?
                PickMultipleFileNative(LocalizationUtil.FileDialogFilterAllNative(true)) :
                await PickMultipleFileUwp(LocalizationUtil.FileDialogFilterAll(true).ToArray());
        }

        public static async Task<string> PickSingleFileUwp(string[] filter)
        {
            var filePicker = new FileOpenPicker();
            filePicker.SetOwnerWindow(App.Services.GetRequiredService<MainWindow>());
            foreach (var item in filter)
            {
                filePicker.FileTypeFilter.Add(item);
            }
            return (await filePicker.PickSingleFileAsync())?.Path;
        }

        public static async Task<IReadOnlyList<string>> PickMultipleFileUwp(string[] filter)
        {
            var filePicker = new FileOpenPicker();
            foreach (var item in filter)
            {
                filePicker.FileTypeFilter.Add(item);
            }
            filePicker.SetOwnerWindow(App.Services.GetRequiredService<MainWindow>());
            var files = await filePicker.PickMultipleFilesAsync();
            return files.Any() ? files.Select(x => x.Path).ToList() : new List<string>();
        }

        public static string PickSingleFileNative(string filter)
        {
            var files = ShowOpenFileDialog(filter);
            return files.Any() ? files[0] : null;
        }

        public static IReadOnlyList<string> PickMultipleFileNative(string filter)
        {
            return ShowOpenFileDialog(filter, true);
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
