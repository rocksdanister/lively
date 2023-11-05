using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace Lively.Common
{
    public static class LinkUtil
    {
        public static void OpenBrowser(Uri uri)
        {
            try
            {
                var ps = new ProcessStartInfo(uri.AbsoluteUri)
                {
                    UseShellExecute = true,
                    Verb = "open"
                };
                Process.Start(ps);
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

        public static bool TrySanitizeUrl(string address, out Uri uri)
        {
            uri = null;
            try
            {
                uri = SanitizeUrl(address);
            }
            catch
            {
                return false;
            }
            return true;
        }

        public static string GetLastSegmentUrl(string url)
        {
            try
            {
                var uri = new Uri(url);
                return GetLastSegmentUrl(uri);
            }
            catch
            {
                return url;
            }
        }

        public static string GetLastSegmentUrl(Uri uri)
        {
            try
            {
                var segment = uri.Segments.Last();
                return (segment == "/" || segment == "//") ? uri.Host.Replace("www.", string.Empty) : segment.Replace("/", string.Empty);
            }
            catch
            {
                return string.Empty;
            }
        }
    }
}