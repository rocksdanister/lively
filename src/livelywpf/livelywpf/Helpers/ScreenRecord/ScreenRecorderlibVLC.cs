using LibVLCSharp.Shared;
using System;
using System.IO;
using System.Threading;
using System.Windows;

namespace livelywpf.Helpers
{
    class ScreenRecorderlibVLC : IScreenRecorder
    {
        LibVLC libVLC;
        MediaPlayer mediaPlayer;
        Media media;
        public event EventHandler<ScreenRecorderStatus> RecorderStatus;

        //todo: error handling, finish/rewrite event firing code.
        public void Initialize(string filePath, Rect rect, int fps, int bitrate, bool isAudio, bool isMousePointer)
        {
            LibVLCSharp.Shared.Core.Initialize(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "plugins", "libVLCPlayer", "libvlc", "win-x86"));
            libVLC = new LibVLC();
            mediaPlayer = new MediaPlayer(libVLC);
            media = new Media(libVLC, "screen://", FromType.FromLocation);
            //IMP: When using non 16:9 resolutions capture is producing corrupted file?!
            media.AddOption(":screen-left=" + (int)rect.Left);
            media.AddOption(":screen-top=" + (int)rect.Top);
            media.AddOption(":screen-width=" + (int)rect.Width);
            media.AddOption(":screen-height=" + (int)rect.Height);
            media.AddOption(":screen-fps=24");
            media.AddOption(":sout=#transcode{vcodec=h264,vb=0,scale=0,acodec=mp4a,ab=128,channels=2,samplerate=44100}:file{dst=" + filePath + "}");
            //media.AddOption(":sout=#transcode{vcodec=hevc,vb=0,scale=0,acodec=mp4a,ab=128,channels=2,samplerate=44100}:file{dst=" + savePath + "}");
            media.AddOption(":sout-keep");
        }

        public void StartRecording()
        {
            ThreadPool.QueueUserWorkItem(_ => mediaPlayer?.Play(media));
            RecorderStatus?.Invoke(this, ScreenRecorderStatus.recording);
        }

        public void StopRecording()
        {
            ThreadPool.QueueUserWorkItem(_ => mediaPlayer?.Stop());
            RecorderStatus?.Invoke(this, ScreenRecorderStatus.success);
            Dispose();
        }

        private void Dispose()
        {
            mediaPlayer?.Dispose();
            libVLC?.Dispose();
            media?.Dispose();
        }
    }
}
