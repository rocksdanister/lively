using System;
using System.Collections.Generic;
using System.Text;

namespace Lively.Common.Services
{
    //TODO
    public class NowPlaying : INowPlaying
    {
        public event EventHandler<NowPlayingEventArgs> NowPlayingTrackChanged;
    }
}
