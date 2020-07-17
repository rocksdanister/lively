using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using LibVLCSharp.Shared;

namespace livelywpf
{
    //References: 
    //https://github.com/mfkl/libvlcsharp-samples/blob/master/ScreenRecorder/Program.cs
    //https://wiki.videolan.org/Documentation:Modules/screen/
    public class CapturelibVLC : IDisposable
    {
        LibVLC libVLC;
        MediaPlayer mediaPlayer;
        Media media;

        public void Initialize(string savePath, int width, int height, int left, int top)
        {
            LibVLCSharp.Shared.Core.Initialize();
            libVLC = new LibVLC();
            mediaPlayer = new MediaPlayer(libVLC);
            media = new Media(libVLC, "screen://", FromType.FromLocation);
            media.AddOption(":screen-left=" + left);
            media.AddOption(":screen-top=" + top);
            media.AddOption(":screen-width=" + width);
            media.AddOption(":screen-height=" + height);
            media.AddOption(":screen-fps=24");
            media.AddOption(":sout=#transcode{vcodec=h264,vb=0,scale=0,acodec=mp4a,ab=128,channels=2,samplerate=44100}:file{dst=" + savePath + "}");
            media.AddOption(":sout-keep");
        }

        public void StartRecord()
        {
            ThreadPool.QueueUserWorkItem(_ => mediaPlayer.Play(media));
        }

        public void StopRecord()
        {
            ThreadPool.QueueUserWorkItem(_ => mediaPlayer.Stop());
        }

        public void Dispose()
        {
            mediaPlayer.Dispose();
            libVLC.Dispose();
            media.Dispose();
        }
    }
}
