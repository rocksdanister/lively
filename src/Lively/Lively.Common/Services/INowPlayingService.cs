using System;
using System.Collections.Generic;
using System.Text;

namespace Lively.Common.Services
{
    public interface INowPlayingService
    {
        event EventHandler<NowPlayingModel> NowPlayingTrackChanged;
        void Start();
        void Stop();
    }

    public class NowPlayingModel : EventArgs
    {
        public string AlbumArtist { get; set; }
        public string AlbumTitle { get; set; }
        public int AlbumTrackCount { get; set; }
        public string Artist { get; set; }
        public List<string> Genres { get; set; }
        public string PlaybackType { get; set; }
        public string Subtitle { get; set; }
        public string Thumbnail { get; set; }
        public string Title { get; set; }
        public int TrackNumber { get; set; }
        //public ColorProperties Colors { get; set; }
    }

    //Future use
    public class ColorProperties
    {
        public string PrimaryColor { get; set; }
        public string SecondaryColor { get; set; }
        public string TertiaryColor { get; set; }
        public string TextColor { get; set; }
        public string ComplementaryColor { get; set; }
    }
}
