using NLog;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Linq;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using livelywpf.Helpers.Hardware;
using livelywpf.Helpers.Archive;

namespace livelywpf
{
    //TODO: impl interface when DI is used for logger.
    public static class LogUtil
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        /// <summary>
        /// Write to log current hardware configuration.
        /// </summary>
        public static void LogHardwareInfo()
        {
            var arch = Environment.Is64BitProcess ? "x86" : "x64";
            var container = Constants.ApplicationType.IsMSIX ? "desktop-bridge" : "desktop-native";
            Logger.Info($"\nLively v{Assembly.GetExecutingAssembly().GetName().Version} {arch} {container} {CultureInfo.CurrentCulture.Name}" +
                $"\n{SystemInfo.GetOSInfo()}\n{SystemInfo.GetCpuInfo()}\n{SystemInfo.GetGpuInfo()}\n");
        }

        /// <summary>
        /// Write to log win32 error if GetLastWin32Error returns true.
        /// </summary>
        public static void LogWin32Error(string message,
            [CallerMemberName] string memberName = "",
            [CallerFilePath] string fileName = "",
            [CallerLineNumber] int lineNumber = 0)
        {
            int err = Marshal.GetLastWin32Error();
            if (err != 0)
            {
                Logger.Error($"HRESULT: {err}, {message} at\n{fileName} ({lineNumber})\n{memberName}");
            }
        }

        /// <summary>
        /// Let user create archive file with all the relevant diagnostic files.
        /// </summary>
        public static void ExtractLogFiles()
        {
            var savePath = string.Empty;
            var saveFileDialog1 = new Microsoft.Win32.SaveFileDialog()
            {
                Title = "Select location to save the file",
                Filter = "Compressed archive|*.zip",
                FileName = "lively_log_" + DateTime.Now.ToString("yyyyMMdd_HHmmss", CultureInfo.InvariantCulture) + ".zip",
            };
            if (saveFileDialog1.ShowDialog() == true)
            {
                savePath = saveFileDialog1.FileName;
            }

            if (!string.IsNullOrEmpty(savePath))
            {
                try
                {
                    var files = new List<string>();
                    var logFolder = Constants.CommonPaths.LogDir;
                    if (Directory.Exists(logFolder))
                    {
                        files.AddRange(Directory.GetFiles(logFolder, "*.*", SearchOption.TopDirectoryOnly));
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
                catch (Exception e)
                {
                    Logger.Error(e.ToString());
                }
            }
        }

        #region helper

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

        #endregion //helper
    }
}