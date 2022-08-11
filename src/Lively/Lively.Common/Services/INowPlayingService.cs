using System;
using System.Collections.Generic;
using System.Text;

namespace Lively.Common.Services
{
    public interface INowPlayingService
    {
        event EventHandler<NowPlayingModel> NowPlayingTrackChanged;
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
    }
}
