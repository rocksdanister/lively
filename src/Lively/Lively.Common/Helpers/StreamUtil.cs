using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;

namespace Lively.Common.Helpers
{
    public static class StreamUtil
    {
        public static bool IsSupportedStream(Uri uri)
        {
            bool status = false;
            string host, url, tmp = string.Empty;
            try
            {
                url = uri.AbsoluteUri;
                host = uri.Host;
            }
            catch
            {
                return status;
            }

            switch (host)
            {
                case "youtube.com":
                case "youtu.be":
                case "www.youtu.be":
                case "www.youtube.com":
                    if (TryParseYouTubeVideoIdFromUrl(url, ref tmp))
                        status = true;
                    break;
                case "www.bilibili.com":
                    if (url.Contains("bilibili.com/video/"))
                        status = true;
                    break;
            }
            return status;
        }

        public static bool IsSupportedStream(string url)
        {
            try
            {
                return IsSupportedStream(new Uri(url));
            }
            catch 
            { 
                return false; 
            }
        }

        //ref: https://stackoverflow.com/questions/39777659/extract-the-video-id-from-youtube-url-in-net
        public static bool TryParseYouTubeVideoIdFromUrl(string url, ref string id)
        {
            Uri uri;
            if (!Uri.TryCreate(url, UriKind.Absolute, out uri))
            {
                try
                {
                    uri = new UriBuilder("http", url).Uri;
                }
                catch
                {
                    // invalid url
                    return false;
                }
            }

            string host = uri.Host;
            string[] youTubeHosts = { "www.youtube.com", "youtube.com", "youtu.be", "www.youtu.be" };
            if (!youTubeHosts.Contains(host))
                return false;

            var query = HttpUtility.ParseQueryString(uri.Query);
            if (query.AllKeys.Contains("v"))
            {
                id = Regex.Match(query["v"], @"^[a-zA-Z0-9_-]{11}$").Value;
                return id != string.Empty;
            }
            else if (query.AllKeys.Contains("u"))
            {
                // some urls have something like "u=/watch?v=AAAAAAAAA16"
                id = Regex.Match(query["u"], @"/watch\?v=([a-zA-Z0-9_-]{11})").Groups[1].Value;
                return id != string.Empty;
            }
            else
            {
                // remove a trailing forward space
                var last = uri.Segments.Last().Replace("/", "");
                if (Regex.IsMatch(last, @"^v=[a-zA-Z0-9_-]{11}$"))
                {
                    id = last.Replace("v=", "");
                    return id != string.Empty;
                }

                string[] segments = uri.Segments;
                if (segments.Length > 2 && segments[segments.Length - 2] != "v/" && segments[segments.Length - 2] != "watch/")
                {
                    return false;
                }

                id = Regex.Match(last, @"^[a-zA-Z0-9_-]{11}$").Value;
                return id != string.Empty;
            }
        }

        public static bool TryParseShadertoy(string url, ref string html)
        {
            if (!url.Contains("shadertoy.com/view"))
                return false;

            if (!LinkUtil.TrySanitizeUrl(url, out _))
                return false;

            url = url.Replace("view/", "embed/");
            html = @"<!DOCTYPE html><html lang=""en"" dir=""ltr""> <head> <meta charset=""utf - 8""> 
                    <title>Digital Brain</title> <style media=""screen""> iframe { position: fixed; width: 100%; height: 100%; top: 0; right: 0; bottom: 0;
                    left: 0; z-index; -1; pointer-events: none;  } </style> </head> <body> <iframe width=""640"" height=""360"" frameborder=""0"" 
                    src=" + url + @"?gui=false&t=10&paused=false&muted=true""></iframe> </body></html>";
            return true;
        }
    }
}
