using Lively.Models.Services;
using System;

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
