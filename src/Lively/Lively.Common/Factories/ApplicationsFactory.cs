using Lively.Common.Helpers.Pinvoke;
using Lively.Models;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Text;

namespace Lively.Common.Factories
{
    public class ApplicationsFactory : IApplicationsFactory
    {
        private readonly string cacheDir = Path.Combine(Constants.CommonPaths.TempDir, "icons");

        public ApplicationModel CreateApp(Process process)
        {
            var model = new ApplicationModel();
            try
            {
                //Can throw exception
                model.AppName = process.ProcessName;
                //Workaround: x86 apps cannot access Process.MainModule of x64 apps
                int capacity = 1024;
                var sb = new StringBuilder(capacity);
                if (!NativeMethods.QueryFullProcessImageName(process.Handle, 0, sb, ref capacity))
                    throw new Win32Exception();

                model.AppPath = sb.ToString(0, capacity);
            }
            catch
            {
                //Failed to retrieve process information.
                return null;
            }

            try
            {
                Directory.CreateDirectory(cacheDir);
                var iconPath = Path.Combine(cacheDir, model.AppName);
                if (!File.Exists(iconPath))
                {
                    //Temp cache
                    Icon.ExtractAssociatedIcon(model.AppPath).ToBitmap().Save(iconPath);
                }
                model.AppIcon = iconPath;
            }
            catch { /* Model is still useful without Icon */ }

            return model;
        }

        public ApplicationModel CreateApp(string path)
        {
            var model = new ApplicationModel
            {
                AppName = Path.GetFileNameWithoutExtension(path),
                AppPath = path
            };

            try
            {
                Directory.CreateDirectory(cacheDir);
                var iconPath = Path.Combine(cacheDir, model.AppName);
                if (!File.Exists(iconPath))
                {
                    Icon.ExtractAssociatedIcon(model.AppPath).ToBitmap().Save(iconPath);
                }
                model.AppIcon = iconPath;
            }
            catch { /* Model is still useful without Icon */ }

            return model;
        }
    }
}
