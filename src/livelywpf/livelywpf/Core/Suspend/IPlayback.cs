using System;

namespace livelywpf.Core.Suspend
{
    public interface IPlayback : IDisposable
    {
        void Start();
        void Stop();
        PlaybackState WallpaperPlayback { get; set; }

        event EventHandler<PlaybackState> PlaybackStateChanged;
    }
}