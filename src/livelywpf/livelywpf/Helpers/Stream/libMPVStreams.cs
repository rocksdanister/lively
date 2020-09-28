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
