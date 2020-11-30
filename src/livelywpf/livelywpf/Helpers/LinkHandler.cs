using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace livelywpf.Helpers
{
    public static class LinkHandler
    {
        public static void OpenBrowser(Uri uri)
        {
            try
            {
                if (Program.IsMSIX)
                {
                    _ = Windows.System.Launcher.LaunchUriAsync(uri);
                }
                else
                {
                    var ps = new ProcessStartInfo(uri.AbsoluteUri)
                    {
                        UseShellExecute = true,
                        Verb = "open"
                    };
                    Process.Start(ps);
                }
            }
            catch { }
        }

        public static void OpenBrowser(string address)
        {

            try
            {
                var uri = new Uri(address);
                if (Program.IsMSIX)
                {
                    _ = Windows.System.Launcher.LaunchUriAsync(uri);
                }
                else
                {
                    var ps = new ProcessStartInfo(uri.AbsoluteUri)
                    {
                        UseShellExecute = true,
                        Verb = "open"
                    };
                    Process.Start(ps);
                }
            }
            catch { }
        }
    }
}