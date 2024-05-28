using Lively.Common.Helpers.Archive;
using Lively.Helpers.Hardware;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace Lively.Common.Helpers
{
    public static class LogUtil
    {
        /// <summary>
        /// Returns data stored in class object file.
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public static string PropertyList(object obj)
        {
            try
            {
                var props = obj.GetType().GetProperties();
                var sb = new StringBuilder();
                foreach (var p in props)
                {
                    sb.AppendLine(p.Name + ": " + p.GetValue(obj, null));
                }
                return sb.ToString();
            }
            catch
            {
                return "Failed to retrive properties of object.";
            }
        }

        /// <summary>
        /// Get hardware information
        /// </summary>
        public static string GetHardwareInfo()
        {
            var arch = Environment.Is64BitProcess ? "x64" : "x86";
            var container = Constants.ApplicationType.IsMSIX ? "desktop-bridge" : "desktop-native";
            return $"\nLively v{Assembly.GetEntryAssembly().GetName().Version} {arch} {container} {CultureInfo.CurrentUICulture.Name}" +
                $"\n{SystemInfo.GetOSInfo()}\n{SystemInfo.GetCpuInfo()}\n{SystemInfo.GetGpuInfo()}\n";
        }

        /// <summary>
        /// Return string representation of win32 error.
        /// </summary>
        /// <param name="message"></param>
        /// <param name="memberName"></param>
        /// <param name="fileName"></param>
        /// <param name="lineNumber"></param>
        /// <returns></returns>
        public static string GetWin32Error(string message,
            [CallerMemberName] string memberName = "",
            [CallerFilePath] string fileName = "",
            [CallerLineNumber] int lineNumber = 0)
        {
            int err = Marshal.GetLastWin32Error();
            return $"HRESULT: {err}, {message} at\n{fileName} ({lineNumber})\n{memberName}";
        }

        /// <summary>
        /// Let user create archive file with all the relevant diagnostic files.
        /// </summary>
        public static void ExtractLogFiles(string savePath)
        {
            var files = new List<string>();

            if (Directory.Exists(Constants.CommonPaths.LogDir))
                files.AddRange(Directory.GetFiles(Constants.CommonPaths.LogDir, "*.*", SearchOption.TopDirectoryOnly));

            if (Directory.Exists(Constants.CommonPaths.LogDirUI))
                files.AddRange(Directory.GetFiles(Constants.CommonPaths.LogDirUI, "*.*", SearchOption.TopDirectoryOnly));

            if (File.Exists(Constants.CommonPaths.UserSettingsPath))
                files.Add(Constants.CommonPaths.UserSettingsPath);

            if (File.Exists(Constants.CommonPaths.WallpaperLayoutPath))
                files.Add(Constants.CommonPaths.WallpaperLayoutPath);

            var cefLogFile = Path.Combine(Constants.CommonPaths.TempCefDir, "logfile.txt");
            if (File.Exists(cefLogFile))
                files.Add(cefLogFile);

            /*
            var procFile = Path.Combine(Program.AppDataDir, "temp", "process.txt");
            File.WriteAllLines(procFile, Process.GetProcesses().Select(x => x.ProcessName));
            files.Add(procFile);
            */

            ZipCreate.CreateZip(savePath, new List<ZipCreate.FileData>() 
            {
                new ZipCreate.FileData() 
                {
                    ParentDirectory = Constants.CommonPaths.AppDataDir,
                    Files = files
                }
            });
        }
    }
}