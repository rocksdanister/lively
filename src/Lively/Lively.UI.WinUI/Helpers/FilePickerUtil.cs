using Lively.Common.Helpers.Files;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage.Pickers;

namespace Lively.UI.WinUI.Helpers
{

    //References:
    //https://github.com/microsoft/WindowsAppSDK/issues/2504
    //https://learn.microsoft.com/en-us/previous-versions/dotnet/netframework-4.0/w5tyztk9(v=vs.100)
    public static class FilePickerUtil
    {
        [DllImport("Comdlg32.dll", CharSet = CharSet.Auto)]
        private static extern bool GetOpenFileName([In, Out] OpenFileName ofn);

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        public class OpenFileName
        {
            public int structSize = 0;
            public IntPtr hwnd = IntPtr.Zero;
            public IntPtr hinst = IntPtr.Zero;
            public string filter = null;
            public string custFilter = null;
            public int custFilterMax = 0;
            public int filterIndex = 0;
            public string file = null;
            public int maxFile = 0;
            public string fileTitle = null;
            public int maxFileTitle = 0;
            public string initialDir = null;
            public string title = null;
            public int flags = 0;
            public short fileOffset = 0;
            public short fileExtMax = 0;
            public string defExt = null;
            public int custData = 0;
            public IntPtr pHook = IntPtr.Zero;
            public string template = null;
        }

        public static async Task<string> FilePickerUwp(string[] filter)
        {
            var filePicker = new FileOpenPicker();
            filePicker.SetOwnerWindow(App.Services.GetRequiredService<MainWindow>());
            foreach (var item in filter)
            {
                filePicker.FileTypeFilter.Add(item);
            }
            return (await filePicker.PickSingleFileAsync())?.Path;
        }

        public static string FilePickerNative(string filters)
        {
            var ofn = new OpenFileName();
            ofn.structSize = Marshal.SizeOf(ofn);
            ofn.file = new string(new char[256]);
            ofn.maxFile = ofn.file.Length;
            ofn.fileTitle = new string(new char[64]);
            ofn.maxFileTitle = ofn.fileTitle.Length;
            ofn.filter = filters;
            return GetOpenFileName(ofn) ? ofn.file : string.Empty;
        }
    }
}
