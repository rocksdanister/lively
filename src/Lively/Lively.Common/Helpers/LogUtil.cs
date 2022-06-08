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
    public class LogUtil
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
                return "Failed to retrive properties of config file.";
            }
        }

        /// <summary>
        /// Get hardware information
        /// </summary>
        public static string GetHardwareInfo()
        {
            var arch = Environment.Is64BitProcess ? "x86" : "x64";
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
            if (string.IsNullOrEmpty(savePath))
            {
                throw new ArgumentNullException(savePath);
            }

            var files = new List<string>();
            var logFolder = Constants.CommonPaths.LogDir;
            if (Directory.Exists(logFolder))
            {
                files.AddRange(Directory.GetFiles(logFolder, "*.*", SearchOption.TopDirectoryOnly));
            }

            var logFolderUI = Constants.CommonPaths.LogDirUI;
            if (Directory.Exists(logFolder))
            {
                files.AddRange(Directory.GetFiles(logFolderUI, "*.*", SearchOption.TopDirectoryOnly));
            }

            var settingsFile = Constants.CommonPaths.UserSettingsPath;
            if (File.Exists(settingsFile))
            {
                files.Add(settingsFile);
            }

            var layoutFile = Constants.CommonPaths.WallpaperLayoutPath;
            if (File.Exists(layoutFile))
            {
                files.Add(layoutFile);
            }

            /*
            var procFile = Path.Combine(Program.AppDataDir, "temp", "process.txt");
            File.WriteAllLines(procFile, Process.GetProcesses().Select(x => x.ProcessName));
            files.Add(procFile);
            */

            if (files.Count != 0)
            {
                ZipCreate.CreateZip(savePath,
                    new List<ZipCreate.FileData>() {
                                new ZipCreate.FileData() { ParentDirectory = Constants.CommonPaths.AppDataDir, Files = files } });
            }
        }
    }
}