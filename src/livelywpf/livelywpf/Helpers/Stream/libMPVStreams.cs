using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;

namespace livelywpf.Helpers
{
    public static class libMPVStreams
    {
        public static bool CheckStream(Uri uri)
        {
            bool status = false;
            var mpvLibPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "plugins", "libMPVPlayer", "lib");

            string host;
            string url;
            try
            {
                url = uri.ToString();
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
                    if (GetYouTubeVideoIdFromUrl(uri, false) != "")
                        status = File.Exists(Path.Combine(mpvLibPath, "youtube-dl.exe"));
                    break;
                case "www.bilibili.com":
                    if (url.Contains("bilibili.com/video/"))
                        status = File.Exists(Path.Combine(mpvLibPath, "youtube-dl.exe"));
                    break;
            }
            return status;
        }

        //ref: https://stackoverflow.com/questions/39777659/extract-the-video-id-from-youtube-url-in-net
        public static string GetYouTubeVideoIdFromUrl(string url)
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
                    return "";
                }
            }

            string host = uri.Host;
            string[] youTubeHosts = { "www.youtube.com", "youtube.com", "youtu.be", "www.youtu.be" };
            if (!youTubeHosts.Contains(host))
                return "";

            var query = HttpUtility.ParseQueryString(uri.Query);
            if (query.AllKeys.Contains("v"))
            {
                return Regex.Match(query["v"], @"^[a-zA-Z0-9_-]{11}$").Value;
            }
            else if (query.AllKeys.Contains("u"))
            {
                // some urls have something like "u=/watch?v=AAAAAAAAA16"
                return Regex.Match(query["u"], @"/watch\?v=([a-zA-Z0-9_-]{11})").Groups[1].Value;
            }
            else
            {
                // remove a trailing forward space
                var last = uri.Segments.Last().Replace("/", "");
                if (Regex.IsMatch(last, @"^v=[a-zA-Z0-9_-]{11}$"))
                    return last.Replace("v=", "");

                string[] segments = uri.Segments;
                if (segments.Length > 2 && segments[segments.Length - 2] != "v/" && segments[segments.Length - 2] != "watch/")
                    return "";

                return Regex.Match(last, @"^[a-zA-Z0-9_-]{11}$").Value;
            }
        }

        private static string GetYouTubeVideoIdFromUrl(Uri uri, bool checkHost = true)
        {
            if(checkHost)
            {
                string host = uri.Host;
                string[] youTubeHosts = { "www.youtube.com", "youtube.com", "youtu.be", "www.youtu.be" };
                if (!youTubeHosts.Contains(host))
                    return "";
            }

            var query = HttpUtility.ParseQueryString(uri.Query);
            if (query.AllKeys.Contains("v"))
            {
                return Regex.Match(query["v"], @"^[a-zA-Z0-9_-]{11}$").Value;
            }
            else if (query.AllKeys.Contains("u"))
            {
                // some urls have something like "u=/watch?v=AAAAAAAAA16"
                return Regex.Match(query["u"], @"/watch\?v=([a-zA-Z0-9_-]{11})").Groups[1].Value;
            }
            else
            {
                // remove a trailing forward space
                var last = uri.Segments.Last().Replace("/", "");
                if (Regex.IsMatch(last, @"^v=[a-zA-Z0-9_-]{11}$"))
                    return last.Replace("v=", "");

                string[] segments = uri.Segments;
                if (segments.Length > 2 && segments[segments.Length - 2] != "v/" && segments[segments.Length - 2] != "watch/")
                    return "";

                return Regex.Match(last, @"^[a-zA-Z0-9_-]{11}$").Value;
            }
        }

        /// <summary>
        /// Returns commandline argument for youtube-dl + mpv player.
        /// </summary>
        public static string YoutubeDLArgGenerate(StreamQualitySuggestion qualitySuggestion, string link)
        {
            string quality = null;
            switch (qualitySuggestion)
            {
                case StreamQualitySuggestion.Lowest:
                    quality = " --ytdl-format bestvideo[height<=480]+bestaudio/best[height<=144]";
                    break;
                case StreamQualitySuggestion.Low:
                    quality = " --ytdl-format bestvideo[height<=480]+bestaudio/best[height<=240]";
                    break;
                case StreamQualitySuggestion.LowMedium:
                    quality = " --ytdl-format bestvideo[height<=480]+bestaudio/best[height<=360]";
                    break;
                case StreamQualitySuggestion.Medium:
                    quality = " --ytdl-format bestvideo[height<=480]+bestaudio/best[height<=480]";
                    break;
                case StreamQualitySuggestion.MediumHigh:
                    quality = " --ytdl-format bestvideo[height<=720]+bestaudio/best[height<=720]";
                    break;
                case StreamQualitySuggestion.High:
                    quality = " --ytdl-format bestvideo[height<=1080]+bestaudio/best[height<=1080]";
                    break;
                case StreamQualitySuggestion.Highest:
                    quality = String.Empty;
                    break;
            }
            return "\"" + link + "\"" + " --force-window=yes --loop-file --keep-open --hwdec=yes --no-keepaspect" + quality;
        }
    }
}
