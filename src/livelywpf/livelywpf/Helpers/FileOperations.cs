using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace livelywpf
{
    public static class FileOperations
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        /// <summary>
        /// Opens the folder in file explorer.
        /// </summary>
        /// <param name="folderPath"></param>
        public static void OpenFolder(string folderPath)
        {
            try
            {
                if (Directory.Exists(folderPath))
                {
                    ProcessStartInfo startInfo = new ProcessStartInfo
                    {
                        Arguments = "\"" + folderPath + "\"",
                        FileName = "explorer.exe"
                    };
                    Process.Start(startInfo);
                }
            }
            catch(Exception e)
            {
                Logger.Error(e.Message);
            }
        }

        /// <summary>
        /// Deletes file amd folder contents of a directory (parent directory remains).
        /// </summary>
        /// <param name="directory"></param>
        /// <returns>true if succes, false otherwise.</returns>
        public static bool EmptyDirectory(string directory)
        {
            try
            {
                System.IO.DirectoryInfo di = new DirectoryInfo(directory);

                foreach (FileInfo file in di.EnumerateFiles())
                {
                    file.Delete();
                }

                foreach (DirectoryInfo dir in di.EnumerateDirectories())
                {
                    dir.Delete(true);
                }
            }
            catch (Exception e)
            {
                Logger.Error(e.Message);
                return false;
            }
            return true;
        }

        /// <summary>
        /// Async folder delete operation after given delay.
        /// </summary>
        /// <param name="folderPath"></param>
        /// <param name="initialDelay"></param>
        /// <param name="retryDelay"></param>
        /// <returns>true if succes, false otherwise.</returns>
        public static async Task<bool> DeleteDirectoryAsync(string folderPath, int initialDelay = 1000, int retryDelay = 4000)
        {
            bool status = false;
            if (Directory.Exists(folderPath))
            {
                await Task.Delay(initialDelay); 
                try
                {
                    await Task.Run(() => Directory.Delete(folderPath, true));
                    status = true;
                }
                catch (Exception ex)
                {
                    Logger.Error("Folder Delete Failure:- " + ex.Message);
                    await Task.Delay(retryDelay);
                    try
                    {
                        await Task.Run(() => Directory.Delete(folderPath, true));
                        status = true;
                    }
                    catch (Exception ie)
                    {
                        Logger.Error("Folder Delete Failure:- " + ie.Message);
                    }
                }
            }
            return status;
        }
    }
}
