using Lively.Common.API;
using Lively.Common.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace Lively.Common.Services
{
    public interface INowPlayingService
    {
        event EventHandler<NowPlayingEventArgs> NowPlayingTrackChanged;
        NowPlayingEventArgs CurrentTrack { get; }
        void Start();
        void Stop();
    }
}
