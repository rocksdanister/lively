using Lively.Models.Enums;
using System;

namespace Lively.Core.Suspend
{
    public interface IPlayback : IDisposable
    {
        void Start();
        void Stop();
        PlaybackState WallpaperPlayback { get; set; }

        event EventHandler<PlaybackState> PlaybackStateChanged;
    }
}