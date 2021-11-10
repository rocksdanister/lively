using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;

namespace livelywpf.Helpers.NetStream
{
    public static class StreamHelper
    {
        public static bool IsSupportedStream(Uri uri)
        {
            bool status = false;
            string host;
            string url;
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
                    if (GetYouTubeVideoIdFromUrl(uri, false) != "")
                        status = true;
                    break;
                case "www.bilibili.com":
                    if (url.Contains("bilibili.com/video/"))
                        status = true;
                    break;
                case "twitch.tv":
                case "www.twitch.tv":
                    if (url.Length != "https://www.twitch.tv/".Length)
                        status = true;
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

        public static bool IsYoutubeUrl(string url) => (GetYouTubeVideoIdFromUrl(url) != "");

        private static string GetYouTubeVideoIdFromUrl(Uri uri, bool checkHost = true)
        {
            if (checkHost)
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
        /// Commandline argument for youtube-dl + mpv player.
        /// </summary>
        public static string YoutubeDLMpvArgGenerate(StreamQualitySuggestion qualitySuggestion, string link)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(link);
            sb.Append(qualitySuggestion switch
                {
                    StreamQualitySuggestion.Lowest => " --ytdl-format=bestvideo[height<=144]+bestaudio/best",
                    StreamQualitySuggestion.Low => " --ytdl-format=bestvideo[height<=240]+bestaudio/best",
                    StreamQualitySuggestion.LowMedium => " --ytdl-format=bestvideo[height<=360]+bestaudio/best",
                    StreamQualitySuggestion.Medium => " --ytdl-format=bestvideo[height<=480]+bestaudio/best",
                    StreamQualitySuggestion.MediumHigh => " --ytdl-format=bestvideo[height<=720]+bestaudio/best",
                    StreamQualitySuggestion.High => " --ytdl-format=bestvideo[height<=1080]+bestaudio/best",
                    StreamQualitySuggestion.Highest => " --ytdl-format=bestvideo+bestaudio/best",
                    _ => string.Empty,
                }
            );
            return sb.ToString();
        }
    }
}
