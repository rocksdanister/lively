using Lively.Common;
using Lively.Common.Helpers.Pinvoke;
using Lively.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lively.UI.WinUI.Factories
{
    public class ApplicationsFactory : IApplicationsFactory
    {
        private readonly string cacheDir = Path.Combine(Constants.CommonPaths.TempDir, "icons");

        public ApplicationModel CreateApp(Process process)
        {
            var model = new ApplicationModel
            {
                AppName = process.ProcessName
            };

            try
            {
                int capacity = 1024;
                var sb = new StringBuilder(capacity);
                //Workaround: x86 apps cannot access Process.MainModule of x64 apps
                NativeMethods.QueryFullProcessImageName(process.Handle, 0, sb, ref capacity);
                model.AppPath = sb.ToString(0, capacity);

                Directory.CreateDirectory(cacheDir);
                var iconPath = Path.Combine(cacheDir, model.AppName);
                if (!File.Exists(iconPath))
                {
                    //temp cache
                    Icon.ExtractAssociatedIcon(model.AppPath).ToBitmap().Save(iconPath);
                }
                model.AppIcon = iconPath;
            }
            catch { }

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
            catch { }

            return model;
        }
    }
}
