using ScreenRecorderLib;
using System;
using System.Windows;

namespace livelywpf.Helpers.ScreenRecord
{
    //todo: make the configuration for video encoding external json file.
    class ScreenRecorderlibScreen : IScreenRecorder
    {
        private string filePath;
        private Recorder _rec;
        private RecorderOptions options;
        public event EventHandler<ScreenRecorderStatus> RecorderStatus;
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        public void Initialize(string filePath, Rect rect, int fps, int bitrate, bool isAudio, bool isMousePointer)
        {
            this.filePath = filePath;
            options = new RecorderOptions
            {
                RecorderMode = RecorderMode.Video,
                //If throttling is disabled, out of memory exceptions may eventually crash the program,
                //depending on encoder settings and system specifications.
                IsThrottlingDisabled = false,
                //Hardware encoding is enabled by default.
                IsHardwareEncodingEnabled = true,
                //Low latency mode provides faster encoding, but can reduce quality.
                IsLowLatencyEnabled = false,
                //Fast start writes the mp4 header at the beginning of the file, to facilitate streaming.
                IsMp4FastStartEnabled = false,
                RecorderApi = RecorderApi.DesktopDuplication,
                AudioOptions = new AudioOptions
                {
                    IsAudioEnabled = isAudio,
                },
                MouseOptions = new MouseOptions
                {
                    IsMousePointerEnabled = isMousePointer,
                },
                VideoOptions = new VideoOptions
                {
                    BitrateMode = BitrateControlMode.UnconstrainedVBR,
                    Bitrate = bitrate, 
                    Framerate = fps,
                    IsFixedFramerate = true,
                    EncoderProfile = H264Profile.Main,
                },
                DisplayOptions = new DisplayOptions()
                {
                    Left = (int)rect.Left,
                    Top = (int)rect.Top,
                    Bottom = (int)rect.Bottom,
                    Right = (int)rect.Right,
                },
            };
        }

        public void Initialize(string filePath, IntPtr hwnd, int fps, int bitrate, bool isAudio, bool isMousePointer)
        {
            this.filePath = filePath;
            options = new RecorderOptions
            {
                RecorderMode = RecorderMode.Video,
                RecorderApi = RecorderApi.WindowsGraphicsCapture,
                DisplayOptions = new DisplayOptions
                {
                    WindowHandle = hwnd
                },
            };
        }

        public void StartRecording()
        {
            _rec = Recorder.CreateRecorder(options);
            _rec.OnRecordingComplete += Rec_OnRecordingComplete;
            _rec.OnRecordingFailed += Rec_OnRecordingFailed;
            _rec.OnStatusChanged += Rec_OnStatusChanged;
            _rec.Record(filePath);
        }

        public void StopRecording()
        {
            _rec?.Stop();
            _rec?.Dispose();
        }

        private void Rec_OnRecordingComplete(object sender, RecordingCompleteEventArgs e)
        {
            RecorderStatus?.Invoke(this, ScreenRecorderStatus.success);
        }

        private void Rec_OnRecordingFailed(object sender, RecordingFailedEventArgs e)
        {
            Logger.Error(e.Error);
            RecorderStatus?.Invoke(this, ScreenRecorderStatus.fail);
        }

        private void Rec_OnStatusChanged(object sender, RecordingStatusEventArgs e)
        {
            switch (e.Status)
            {
                case ScreenRecorderLib.RecorderStatus.Idle:
                    RecorderStatus?.Invoke(this, ScreenRecorderStatus.idle);
                    break;
                case ScreenRecorderLib.RecorderStatus.Recording:
                    RecorderStatus?.Invoke(this, ScreenRecorderStatus.recording);
                    break;
                case ScreenRecorderLib.RecorderStatus.Paused:
                    RecorderStatus?.Invoke(this, ScreenRecorderStatus.paused);
                    break;
                case ScreenRecorderLib.RecorderStatus.Finishing:
                    RecorderStatus?.Invoke(this, ScreenRecorderStatus.finishing);
                    break;
            }
        }
    }
}
