using NLog;
using System;
using System.Windows.Threading;
using Windows.Media.Control;

namespace livelywpf.Helpers
{
    public class NowPlayingEventArgs : EventArgs
    {
        /// <summary>
        /// Song title.
        /// </summary>
        public string Title { get; set; }
        /// <summary>
        /// Song artist.
        /// </summary>
        public string Artist { get; set; }
    }

    public sealed class NowPlaying
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
        public event EventHandler<NowPlayingEventArgs> NowPlayingTrackChanged = delegate {};
        private static readonly NowPlaying instance = new NowPlaying();
        private readonly DispatcherTimer dispatcherTimer;
        private readonly bool fireOnlyIfTrackChange = false;
        private NowPlayingEventArgs previousTrack = null;

        public static NowPlaying Instance
        {
            get
            {
                return instance;
            }
        }

        private NowPlaying()
        {
            dispatcherTimer = new DispatcherTimer();
            dispatcherTimer.Tick += new EventHandler(TimerFunc);
            dispatcherTimer.Interval = new TimeSpan(0, 0, 0, 0, 1000);
            dispatcherTimer.Start();
            StartService();
        }

        public void StartService()
        {
            if (!dispatcherTimer.IsEnabled)
            {
                dispatcherTimer.Start();
                Logger.Info("NowPlaying: Started.");
            }
        }

        public void StopService()
        {
            //If subscribers are none, turn off.
            //todo: make it thread safe.
            if (NowPlayingTrackChanged?.GetInvocationList().Length == 1)
            {
                dispatcherTimer.Stop();
                Logger.Info("NowPlaying: Stopped");
            }
        }

        private void TimerFunc(object sender, EventArgs e)
        {
            if(fireOnlyIfTrackChange)
            {
                var currTrack = GetCurrentTrackInfo();
                if (previousTrack == null)
                {
                    previousTrack = currTrack;
                    NowPlayingTrackChanged?.Invoke(this, GetCurrentTrackInfo());
                }
                else if (currTrack.Artist != previousTrack.Artist || currTrack.Title != previousTrack.Title)
                {
                    previousTrack = currTrack;
                    NowPlayingTrackChanged?.Invoke(this, GetCurrentTrackInfo());
                }
            }
            else
            {
                NowPlayingTrackChanged?.Invoke(this, GetCurrentTrackInfo());
            }
        }

        public NowPlayingEventArgs GetCurrentTrackInfo()
        {
            var trackInfo = new NowPlayingEventArgs() { Artist = "Nil", Title = "Nil" };
            try
            {
                var gsmtcsm = GlobalSystemMediaTransportControlsSessionManager.RequestAsync().GetAwaiter().GetResult().GetCurrentSession();
                if(gsmtcsm != null)
                {
                    var mediaProperties = gsmtcsm.TryGetMediaPropertiesAsync().GetAwaiter().GetResult();
                    trackInfo.Artist = mediaProperties.Artist;
                    trackInfo.Title = mediaProperties.Title;
                }
            }
            catch { }
            return trackInfo;
        }
    }
}
