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
        public event EventHandler<NowPlayingEventArgs> NowPlayingTrackChanged = delegate {};
        private static readonly NowPlaying instance = new NowPlaying();
        private readonly DispatcherTimer dispatcherTimer;
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
            //dispatcherTimer.Start();
        }

        public void Start()
        {
            if (!dispatcherTimer.IsEnabled)
            {
                dispatcherTimer.Start();
            }
        }

        public void Stop()
        {
            if(NowPlayingTrackChanged?.GetInvocationList().Length == 1)
            {
                dispatcherTimer.Stop();
            }
        }

        private void TimerFunc(object sender, EventArgs e)
        {
            var currTrack = GetCurrentTrackInfo();
            if(previousTrack == null)
            {
                previousTrack = currTrack;
                NowPlayingTrackChanged?.Invoke(null, GetCurrentTrackInfo());
            }
            else if(currTrack.Artist != previousTrack.Artist || currTrack.Title != previousTrack.Title)
            {
                previousTrack = currTrack;
                NowPlayingTrackChanged?.Invoke(null, GetCurrentTrackInfo());
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
