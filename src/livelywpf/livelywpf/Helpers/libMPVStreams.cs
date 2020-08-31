using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace livelywpf.Helpers
{
    public static class libMPVStreams
    {
        public static bool CheckStream(Uri uri)
        {
            bool status = true;
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
                return false;
            }

            switch (host)
            {
                case "www.youtube.com":
                    if (url.Contains("youtube.com/watch?v="))
                        status = File.Exists(Path.Combine(mpvLibPath, "youtube-dl.exe"));
                    break;
                case "www.bilibili.com":
                    if (url.Contains("bilibili.com/video/"))
                        status = File.Exists(Path.Combine(mpvLibPath, "youtube-dl.exe"));
                    break;
                default:
                    status = false;
                    break;
            }
            return status;
        }

        /*
        /// <summary>
        /// Returns commandline argument for youtube-dl + mpv.
        /// </summary>
        private static string YoutubeDLArgGenerate(StreamQualitySuggestion qualitySuggestion, string link)
        {
            string quality = null;
            switch (qualitySuggestion)
            {
                case StreamQualitySuggestion.best:
                    quality = String.Empty;
                    break;
                case StreamQualitySuggestion.h2160p:
                    quality = " --ytdl-format bestvideo[height<=2160]+bestaudio/best[height<=2160]";
                    break;
                case StreamQualitySuggestion.h1440p:
                    quality = " --ytdl-format bestvideo[height<=1440]+bestaudio/best[height<=1440]";
                    break;
                case StreamQualitySuggestion.h1080p:
                    quality = " --ytdl-format bestvideo[height<=1080]+bestaudio/best[height<=1080]";
                    break;
                case StreamQualitySuggestion.h720p:
                    quality = " --ytdl-format bestvideo[height<=720]+bestaudio/best[height<=720]";
                    break;
                case StreamQualitySuggestion.h480p:
                    quality = " --ytdl-format bestvideo[height<=480]+bestaudio/best[height<=480]";
                    break;
                default:
                    quality = " --ytdl-format bestvideo[height<=720]+bestaudio/best[height<=720]";
                    break;
            }
            return "\"" + link + "\"" + " --force-window=yes --loop-file --keep-open --hwdec=yes --no-keepaspect" + quality;
        }
        */
    }
}
