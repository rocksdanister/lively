using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace livelywpf.Helpers.Files
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
                //bug, disabled for now.
                if (Constants.ApplicationType.IsMSIX)
                {
                    _ = LaunchFolder(path);
                    return;
                }

                ProcessStartInfo startInfo = new ProcessStartInfo
                {
                    FileName = "explorer.exe"
                };
                if (File.Exists(path))
                {
                    startInfo.Arguments = "/select, \"" + path + "\"";
                }
                else if (Directory.Exists(path))
                {
                    startInfo.Arguments = "\"" + path + "\"";
                }
                else
                {
                    throw new FileNotFoundException();
                }
                Process.Start(startInfo);
            }
            catch (Exception e)
            {
                Logger.Error(e.Message + "\n" + e.StackTrace);
            }
        }

        private static async Task LaunchFolder(string path)
        {
            var packagePath = path;
            var localFolder = Windows.Storage.ApplicationData.Current.LocalCacheFolder.Path;
            var packageAppData = Path.Combine(localFolder, "Local", "Lively Wallpaper");
            if (path.Length > Constants.CommonPaths.AppDataDir.Count() + 1)
            {
                var tmp = Path.Combine(packageAppData, path.Remove(0, Constants.CommonPaths.AppDataDir.Count() + 1));
                if (File.Exists(tmp) || Directory.Exists(tmp))
                {
                    packagePath = tmp;
                }
            }

            var folder = await Windows.Storage.StorageFolder.GetFolderFromPathAsync(Path.GetDirectoryName(packagePath));
            await Windows.System.Launcher.LaunchFolderAsync(folder);
        }

        /// <summary>
        /// Replaces invalid filename characters with "_" character.
        /// </summary>
        /// <param name="filename">Filename.</param>
        /// <returns>Valid filename</returns>
        public static string GetSafeFilename(string filename)
        {
            return string.Join("_", filename.Split(Path.GetInvalidFileNameChars()));
        }

        //ref: https://stackoverflow.com/questions/1078003/c-how-would-you-make-a-unique-filename-by-adding-a-number
        public static string NextAvailableFilename(string path)
        {
            // Short-cut if already available
            if (!File.Exists(path))
                return path;

            var numberPattern = " ({0})";

            // If path has extension then insert the number pattern just before the extension and return next filename
            if (Path.HasExtension(path))
                return GetNextFilename(path.Insert(path.LastIndexOf(Path.GetExtension(path)), numberPattern));

            // Otherwise just append the pattern to the path and return next filename
            return GetNextFilename(path + numberPattern);
        }

        private static string GetNextFilename(string pattern)
        {
            string tmp = string.Format(pattern, 1);
            if (tmp == pattern)
                throw new ArgumentException("The pattern must include an index place-holder", "pattern");

            if (!File.Exists(tmp))
                return tmp; // short-circuit if no matches

            int min = 1, max = 2; // min is inclusive, max is exclusive/untested

            while (File.Exists(string.Format(pattern, max)))
            {
                min = max;
                max *= 2;
            }

            while (max != min + 1)
            {
                int pivot = (max + min) / 2;
                if (File.Exists(string.Format(pattern, pivot)))
                    min = pivot;
                else
                    max = pivot;
            }

            return string.Format(pattern, max);
        }

        /// <summary>
        /// Calculates file checksum.
        /// </summary>
        /// <param name="filePath">Path of file.</param>
        /// <returns>SHA256 checksum.</returns>
        public static string GetChecksumSHA256(string filePath)
        {
            using SHA256 sha256 = SHA256.Create();
            using var stream = File.OpenRead(filePath);
            var hash = sha256.ComputeHash(stream);
            return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
        }

        /// <summary>
        /// Deletes file and folder contents of a directory (parent directory remains).
        /// </summary>
        /// <param name="directory"></param>
        /// <returns>True if deletion completed succesfully.</returns>
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
        /// <returns>True if deletion completed succesfully.</returns>
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
                    Logger.Error("Folder Delete Failure {0}.\nRetrying..", ex.Message);
                    await Task.Delay(retryDelay);
                    try
                    {
                        await Task.Run(() => Directory.Delete(folderPath, true));
                    }
                    catch (Exception ie)
                    {
                        Logger.Error("(Retry)Folder Delete Failure: {0}", ie.Message);
                        status = false;
                    }
                }
            }
            return status;
        }

        //ref: https://docs.microsoft.com/en-us/dotnet/standard/io/how-to-copy-directories
        /// <summary>
        /// Directory copy operation.
        /// </summary>
        /// <param name="sourceDirName"></param>
        /// <param name="destDirName"></param>
        /// <param name="copySubDirs"></param>
        public static void DirectoryCopy(string sourceDirName, string destDirName, bool copySubDirs)
        {
            // Get the subdirectories for the specified directory.
            DirectoryInfo dir = new DirectoryInfo(sourceDirName);

            if (!dir.Exists)
            {
                throw new DirectoryNotFoundException(
                    "Source directory does not exist or could not be found: "
                    + sourceDirName);
            }

            DirectoryInfo[] dirs = dir.GetDirectories();
            // If the destination directory doesn't exist, create it.
            if (!Directory.Exists(destDirName))
            {
                Directory.CreateDirectory(destDirName);
            }

            // Get the files in the directory and copy them to the new location.
            FileInfo[] files = dir.GetFiles();
            foreach (FileInfo file in files)
            {
                string temppath = Path.Combine(destDirName, file.Name);
                file.CopyTo(temppath, false);
            }

            // If copying subdirectories, copy them and their contents to new location.
            if (copySubDirs)
            {
                foreach (DirectoryInfo subdir in dirs)
                {
                    string temppath = Path.Combine(destDirName, subdir.Name);
                    DirectoryCopy(subdir.FullName, temppath, copySubDirs);
                }
            }
        }
    }
}
