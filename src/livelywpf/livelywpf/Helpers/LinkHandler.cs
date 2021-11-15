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
                if (Constants.ApplicationType.IsMSIX)
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
                OpenBrowser(new Uri(address));
            }
            catch { }
        }

        public static Uri SanitizeUrl(string address)
        {
            if (string.IsNullOrWhiteSpace(address))
            {
                throw new ArgumentException();
            }

            Uri uri;
            try
            {
                uri = new Uri(address);
            }
            catch (UriFormatException)
            {
                //if user did not input https/http assume https connection.
                uri = new UriBuilder(address)
                {
                    Scheme = "https",
                    Port = -1,
                }.Uri;
            }
            return uri;
        }
    }
}