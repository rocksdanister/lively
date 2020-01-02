using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace livelywpf.Lively.Helpers
{
    public static class FileOperations
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
        /// <summary>
        /// Deletes file & folder contents of a directory (directory remains).
        /// </summary>
        /// <param name="directory"></param>
        /// <returns>success =true, error =false</returns>
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
            catch
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Async folder delete operation (Starts with initial delay of 1sec, if fails retry after 4sec).
        /// </summary>
        /// <param name="folderPath"></param>
        public static async void DeleteDirectoryAsync(string folderPath)
        {
            if (Directory.Exists(folderPath))
            {
                await Task.Delay(1000); //todo:- find if gif is dealloacted & do this more elegantly.(xamlanimatedgif preview)
                try
                {
                    await Task.Run(() => Directory.Delete(folderPath, true)); //thread blocking otherwise
                }
                catch (IOException ex1)
                {
                    Logger.Error("IOException: failed to delete wp from library, waiting 4sec for gif to dealloac:" + ex1.ToString());
                    await Task.Delay(4000);
                    try
                    {
                        await Task.Run(() => Directory.Delete(folderPath, true));
                    }
                    catch (Exception ie)
                    {
                        MessageBox.Show("Folder Delete Failure:- " + ie.Message, Properties.Resources.txtLivelyErrorMsgTitle);
                    }
                }
                catch (Exception ex2)
                {
                    Logger.Error("WP folder delete error:- " + ex2.ToString());
                    MessageBox.Show("Folder Delete Failure:- " + ex2.Message, Properties.Resources.txtLivelyErrorMsgTitle);
                }
            }
        }

    }
}
