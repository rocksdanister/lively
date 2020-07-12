using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace livelywpf
{
    public static class FileOperations
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        /// <summary>
        /// Opens the folder in file explorer; If file path is given, file is selected.
        /// </summary>
        /// <param name="path"></param>
        public static void OpenFolder(string path)
        {
            try
            {
                ProcessStartInfo startInfo = new ProcessStartInfo
                {
                    FileName = "explorer.exe"
                };
                if (File.Exists(path))
                {
                    startInfo.Arguments = "/select, \"" + path + "\"";
                }
                else if(Directory.Exists(path))
                {
                    startInfo.Arguments = "\"" + path + "\"";
                }
                else
                {
                    throw new FileNotFoundException();
                }
                Process.Start(startInfo);
            }
            catch(Exception e)
            {
                Logger.Error(e.Message + "\n" + e.StackTrace);
            }
        }

        /// <summary>
        /// Deletes file amd folder contents of a directory (parent directory remains).
        /// </summary>
        /// <param name="directory"></param>
        /// <returns>true if succes, false otherwise.</returns>
        public static bool EmptyDirectory(string directory)
        {
            var status = true;
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
                Logger.Error(e.ToString());
                status = false;
            }
            return status;
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
            bool status = true;
            if (Directory.Exists(folderPath))
            {
                await Task.Delay(initialDelay); 
                try
                {
                    await Task.Run(() => Directory.Delete(folderPath, true));
                }
                catch (Exception ex)
                {
                    Logger.Error("Folder Delete Failure: " + ex.Message + "\n" +"Retrying..");
                    await Task.Delay(retryDelay);
                    try
                    {
                        await Task.Run(() => Directory.Delete(folderPath, true));
                    }
                    catch (Exception ie)
                    {
                        Logger.Error("(Retry)Folder Delete Failure: " + ie.Message);
                        status = false;
                    }
                }
            }
            return status;
        }

        /// <summary>
        /// Check if the file is video.
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public static bool IsVideoFile(string path)
        {
            bool status = false;
            string[] formatsVideo = { ".dat", ".wmv", ".3g2", ".3gp", ".3gp2", ".3gpp", ".amv", ".asf",  ".avi", ".bin", ".cue", ".divx", ".dv", ".flv", ".gxf", ".iso", ".m1v", ".m2v", ".m2t", ".m2ts", ".m4v",
                                      ".mkv", ".mov", ".mp2", ".mp2v", ".mp4", ".mp4v", ".mpa", ".mpe", ".mpeg", ".mpeg1", ".mpeg2", ".mpeg4", ".mpg", ".mpv2", ".mts", ".nsv", ".nuv", ".ogg", ".ogm", ".ogv", 
                                      ".ogx", ".ps", ".rec", ".rm",".rmvb", ".tod", ".ts", ".tts", ".vob", ".vro", ".webm" };
            if (formatsVideo.Contains(Path.GetExtension(path), StringComparer.OrdinalIgnoreCase))
            {
                status = true;
            }
            return status;
        }
    }
}
