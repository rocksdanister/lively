using CommandLine;
using LibVLCSharp.Shared;
using Lively.Common.Extensions;
using Lively.Common.Helpers;
using Lively.Common.JsonConverters;
using Lively.Models.Enums;
using Lively.Models.Message;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Lively.Player.Vlc
{
    public partial class Form1 : Form
    {
        private LibVLC libVLC;
        private StartArgs startArgs;
        private MediaPlayer mediaPlayer;
        private Media media;

        private WallpaperScaler CurrentScaler { get; set; } = WallpaperScaler.uniform;
        private bool IsDebugging { get; } = BuildInfoUtil.IsDebugBuild();

        public Form1()
        {
            InitializeComponent();
            if (IsDebugging)
            {
                startArgs = new StartArgs()
                {
                    FilePath = "",
                    Properties = "",
                    Volume = 100,
                    HardwareDecoding = true
                };

                this.FormBorderStyle = FormBorderStyle.Sizable;
                this.WindowState = FormWindowState.Normal;
                this.StartPosition = FormStartPosition.Manual;
                this.Size = new Size(1920, 1080);
                this.ShowInTaskbar = true;
                this.MaximizeBox = true;
                this.MinimizeBox = true;
            }
            else
            {
                Parser.Default.ParseArguments<StartArgs>(Environment.GetCommandLineArgs())
                  .WithParsed((x) => startArgs = x)
                  .WithNotParsed(HandleParseError);

                this.WindowState = FormWindowState.Minimized;
                this.StartPosition = FormStartPosition.Manual;
                this.Location = new Point(-9999, 0);

                if (startArgs.Geometry != null)
                {
                    var msg = startArgs.Geometry.Split('x');
                    if (msg.Length >= 2 && int.TryParse(msg[0], out int width) && int.TryParse(msg[1], out int height))
                    {
                        this.Size = new Size(width, height);
                    }
                }

                var darkColor = Color.FromArgb(30, 30, 30);
                var lightColor = Color.FromArgb(240, 240, 240);
                this.BackColor = startArgs.Theme switch
                {
                    AppTheme.Auto => ThemeUtil.GetWindowsTheme() == AppTheme.Dark ? darkColor : lightColor,
                    AppTheme.Light => lightColor,
                    AppTheme.Dark => darkColor,
                    _ => darkColor,
                };
            }
        }

        // Hide from taskview and taskbar.
        // ShowInTaskbar = true does not create TOOLWINDOW.
        protected override CreateParams CreateParams
        {
            get
            {
                var cp = base.CreateParams;
                cp.ExStyle |= 0x00000080; // WS_EX_TOOLWINDOW
                cp.ExStyle |= 0x08000000; // WS_EX_NOACTIVATE
                return cp;
            }
        }

        private void HandleParseError(IEnumerable<Error> errs)
        {
            if (errs != null)
                string.Join(Environment.NewLine, errs).SendError(SendToParent, "Error parsing launch arguments");

            // ERROR_INVALID_PARAMETER
            // Ref: <https://learn.microsoft.com/en-us/windows/win32/debug/system-error-codes--0-499->
            Environment.Exit(87);
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            // We are initializing Vlc in Shown() to avoid blocking.
        }

        private async void Form1_Shown(object sender, EventArgs e)
        {
            SendToParent(new LivelyMessageHwnd() {
                Hwnd = this.Handle.ToInt32()
            });

            try
            {
                await InitializeVlc();

                media = new Media(libVLC, startArgs.FilePath, FromType.FromPath);
                mediaPlayer.Play(media);

                await RestoreLivelyProperties(startArgs.Properties);
                SendToParent(new LivelyMessageWallpaperLoaded() { Success = true });
            }
            catch (Exception ex)
            {
                ex.SendError(SendToParent, "Failed to initialize player");
                // Exit or display custom error page.
                Environment.Exit(1);
            }
            finally
            {
                _ = ListenToParent();
            }
        }

        private async Task InitializeVlc()
        {
            // Blocking operation, give time for the loading picturebox to be visible.
            await Task.Delay(100);
            Core.Initialize();

            // "--no-disable-screensaver" : Enable monitor sleep.
            // "--no-stats" : Disable locally collect statistics.
            // Ref: https://wiki.videolan.org/VLC_command-line_help
            libVLC = new LibVLC("no-disable-screensaver",
                "no-stats",
                "no-osd",
                "no-spu",
                "no-sub-autodetect-file",
                "no-snapshot-preview");
            mediaPlayer = new MediaPlayer(libVLC)
            {
                Volume = startArgs.Volume,
                EnableHardwareDecoding = startArgs.HardwareDecoding,
                EnableKeyInput = false,
                EnableMouseInput = false
            };
            mediaPlayer.EndReached += MediaPlayer_EndReached;
            mediaPlayer.EncounteredError += MediaPlayer_EncounteredError;
            videoView1.MediaPlayer = mediaPlayer;
            pictureBox1.Visible = false;
            videoView1.Visible = true;
            EnableImageOptions(true);
            SetScale(CurrentScaler);
        }

        private void MediaPlayer_EndReached(object sender, EventArgs e)
        {
            // --loop, --inpur-repeat does not work.
            // Alternatively reset position after threshold.
            ThreadPool.QueueUserWorkItem(_ => mediaPlayer.Play(media));
        }

        private void MediaPlayer_EncounteredError(object sender, EventArgs e)
        {
            "MediaPlayer_EncounteredError".SendError(SendToParent);
        }

        public void Play() => mediaPlayer.Play();

        public void Pause() => mediaPlayer.SetPause(true);

        public bool IsPlaying() => mediaPlayer.IsPlaying;

        public void SetVolume(int volume) => mediaPlayer.Volume = volume;

        private void SetPlaybackSpeed(float speed)
        {
            mediaPlayer.SetRate(speed);
        }

        private void SetMute(bool mute)
        {
            mediaPlayer.Mute = mute;
        }

        private void EnableImageOptions(bool isEnable)
        {
            if (!mediaPlayer.EnableHardwareDecoding) {
                "Hardware decoding is disabled, filters turned off for stability.".SendLog(SendToParent);
                return;
            }

            mediaPlayer.SetAdjustInt(VideoAdjustOption.Enable, isEnable ? 1 : 0);
        }

        private void SetImageOption(VideoAdjustOption option, float value)
        {
            // Crash with post-processing with disabled.
            if (!mediaPlayer.EnableHardwareDecoding)
                return;

            mediaPlayer.SetAdjustFloat(option, value);
        }

        public bool CaptureScreenshot(string filePath)
        {
            try
            {
                // Issue: Solid green image.
                // Issue: Unable to remove thumbnail.
                // Ref: https://forum.videolan.org/viewtopic.php?f=14&t=144069&p=546844
                // https://forum.videolan.org/viewtopic.php?f=32&t=146793
                EnableImageOptions(false);
                return mediaPlayer.TakeSnapshot(0, filePath, 0, 0);
            }
            finally
            {
                EnableImageOptions(true);
            }
        }

        private string GetAspectRatio()
        {
            return this.videoView1.Width > 0 && this.videoView1.Height > 0 ? $"{this.videoView1.Width}:{this.videoView1.Height}" : null;
        }

        private void SetScale(WallpaperScaler scaler)
        {
            if (mediaPlayer == null)
                return;

            switch (scaler)
            {
                case WallpaperScaler.none:
                    // Original size, no scaling
                    mediaPlayer.Scale = 1.0f;
                    mediaPlayer.AspectRatio = null;
                    mediaPlayer.CropGeometry = null;
                    break;
                case WallpaperScaler.fill:
                    // Stretch to fill window (may distort)
                    mediaPlayer.Scale = 0f;           // Auto-scale to window
                    mediaPlayer.AspectRatio = GetAspectRatio();
                    mediaPlayer.CropGeometry = null;
                    break;
                case WallpaperScaler.uniformFill:
                    // Fill window keeping aspect ratio (crop sides)
                    mediaPlayer.Scale = 0f;           // Auto-scale to window
                    mediaPlayer.AspectRatio = null;   // Keep original aspect ratio  
                    mediaPlayer.CropGeometry = GetAspectRatio(); // Crop to window ratio
                    break;
                case WallpaperScaler.uniform:
                case WallpaperScaler.auto:
                    // Fit inside window keeping aspect ratio (letterbox)
                    mediaPlayer.Scale = 0f;           // Auto-scale to window
                    mediaPlayer.AspectRatio = null;   // Keep original aspect ratio
                    mediaPlayer.CropGeometry = null;  // No cropping
                    break;
            }
            CurrentScaler = scaler;
        }

        private void Form1_SizeChanged(object sender, EventArgs e)
        {
            SetScale(CurrentScaler);
        }

        public async Task ListenToParent()
        {
            if (IsDebugging)
                return;

            var reader = new StreamReader(Console.OpenStandardInput(), Encoding.UTF8);

            try
            {
                await Task.Run(async () =>
                {
                    while (true)
                    {
                        // Since UTF8 is backward compatible, will work without this reader for non unicode characters.
                        string text = await reader.ReadLineAsync();
                        if (startArgs.VerboseLog)
                            Console.WriteLine(text);

                        if (string.IsNullOrEmpty(text))
                        {
                            // When the redirected stream is closed, a null line is sent to the event handler. 
                            break;
                        }
                        else
                        {
                            try
                            {
                                var close = false;
                                var obj = JsonConvert.DeserializeObject<IpcMessage>(text, new JsonSerializerSettings() { Converters = { new IpcMessageConverter() } });
                                this.Invoke((Action)(async () =>
                                {
                                    switch (obj.Type)
                                    {
                                        case MessageType.cmd_reload:
                                            mediaPlayer.Position = 0f;
                                            break;
                                        case MessageType.cmd_screenshot:
                                            var scr = (LivelyScreenshotCmd)obj;
                                            var success = CaptureScreenshot(scr.FilePath);
                                            SendToParent(new LivelyMessageScreenshot() {
                                                FileName = Path.GetFileName(scr.FilePath),
                                                Success = success
                                            });
                                            break;
                                        case MessageType.lp_slider:
                                            var sl = (LivelySlider)obj;
                                            SetLivelyProperty(sl.Name, sl.Value);
                                            break;
                                        case MessageType.lp_chekbox:
                                            var cb = (LivelyCheckbox)obj;
                                            SetLivelyProperty(cb.Name, cb.Value);
                                            break;
                                        case MessageType.lp_dropdown_scaler:
                                            var dds = (LivelyDropdownScaler)obj;
                                            SetLivelyProperty(dds.Name, dds.Value);
                                            break;
                                        case MessageType.cmd_suspend:
                                            Pause();
                                            break;
                                        case MessageType.cmd_resume:
                                            Play();
                                            break;
                                        case MessageType.cmd_close:
                                            close = true;
                                            break;
                                        case MessageType.cmd_volume:
                                            var vc = (LivelyVolumeCmd)obj;
                                            SetVolume(vc.Volume);
                                            break;
                                        case MessageType.lp_button:
                                            var btn = (LivelyButton)obj;
                                            if (btn.IsDefault)
                                                await RestoreLivelyProperties(startArgs.Properties);
                                            break;
                                    }
                                }));

                                if (close)
                                    break;
                            }
                            catch (Exception ie)
                            {
                                ie.SendError(SendToParent);
                            }
                        }
                    }
                });
            }
            catch (Exception e)
            {
                e.SendError(SendToParent);
            }
            finally
            {
                this.Invoke((Action)Application.Exit);
            }
        }

        private async Task RestoreLivelyProperties(string propertyPath)
        {
            try
            {
                // Disable video adjust filter to prevent state corruption during bulk property updates.
                EnableImageOptions(false);
                await LivelyPropertyUtil.LoadProperty(propertyPath, Path.GetDirectoryName(startArgs.FilePath), async (key, value) =>
                {
                    SetLivelyProperty(key, value);
                });
            }
            catch (Exception ex)
            {
                ex.SendError(SendToParent);
            }
            finally
            {
                // Re-enable the filter after all properties are set.
                EnableImageOptions(true);
            }
        }

        private void SetLivelyProperty(string key, object value)
        {
            // Image properties filter(adjust)
            // --contrast =< float[0.000000..2.000000] >
            //                           Image contrast(0 - 2)
            //     Set the image contrast, between 0 and 2.Defaults to 1.
            // --brightness =< float[0.000000..2.000000] >
            //                            Image brightness(0 - 2)
            //     Set the image brightness, between 0 and 2.Defaults to 1.
            // --hue =< float[-180.000000..180.000000] >
            //                            Image hue(-180..180)
            //     Set the image hue, between -180 and 180.Defaults to 0.
            // --saturation =< float[0.000000..3.000000] >
            //                            Image saturation(0 - 3)
            //     Set the image saturation, between 0 and 3.Defaults to 1.
            // --gamma =< float[0.010000..10.000000] >
            //                            Image gamma(0 - 10)
            //     Set the image gamma, between 0.01 and 10.Defaults to 1.
            // Ref: https://wiki.videolan.org/VLC_command-line_help

            switch (key.ToLower())
            {
                case "saturation":
                    {
                        float inputValue = Convert.ToSingle(value);
                        // Map -100..100 to VLC range 0.0..3.0 (default 1.0)
                        float saturation = MapWithPivot(inputValue, -100f, 100f, 0f, 0f, 3f, 1f);
                        SetImageOption(VideoAdjustOption.Saturation, saturation);
                    }
                    break;
                case "brightness":
                    {
                        float inputValue = Convert.ToSingle(value);
                        // Map -100..100 to VLC range 0.0..2.0 (default 1.0)
                        float brightness = MapWithPivot(inputValue, -100f, 100f, 0f, 0f, 2f, 1f);
                        SetImageOption(VideoAdjustOption.Brightness, brightness);
                    }
                    break;
                case "contrast":
                    {
                        float inputValue = Convert.ToSingle(value);
                        // Map -100..100 to VLC range 0.0..2.0 (default 1.0)
                        float contrast = MapWithPivot(inputValue, -100f, 100f, 0f, 0f, 2f, 1f);
                        SetImageOption(VideoAdjustOption.Contrast, contrast);
                    }
                    break;
                case "hue":
                    {
                        float inputValue = Convert.ToSingle(value);
                        // Map -100..100 to VLC range -180..180 (default 0)
                        float hue = MapWithPivot(inputValue, -100f, 100f, 0f, -180f, 180f, 0f);
                        SetImageOption(VideoAdjustOption.Hue, hue);
                    }
                    break;
                // This filter is not working?
                //case "gamma":
                //    {
                //        float inputValue = Convert.ToSingle(value);
                //        // Map -100..100 to VLC range 0.01..10.0 (default 1.0)
                //        float gamma = MapWithPivot(inputValue, -100f, 100f, 0f, 0.01f, 10f, 1f);
                //        SetImageOption(VideoAdjustOption.Gamma, gamma);
                //    }
                //    break;
                case "speed":
                    {
                        float inputValue = Convert.ToSingle(value);
                        // Speed is already in correct range (0.25 - 5.0).
                        //  --rate=<float [-340282346638528859811704183484516925440.000000 .. 340282346638528859811704183484516925440.000000]>
                        SetPlaybackSpeed(inputValue);
                    }
                    break;
                case "scaler":
                    {
                        var newScale = (WallpaperScaler)value;
                        SetScale(newScale);
                    }
                    break;
                case "mute":
                    {
                        SetMute((bool)value);
                    }
                    break;
                default:
                    $"Unknown livelyproperty: {key}".SendLog(SendToParent);
                    break;
            }
        }

        private void Form1_FormClosed(object sender, FormClosedEventArgs e)
        {
            mediaPlayer?.Stop();
            mediaPlayer?.Dispose();
            libVLC?.Dispose();
            media?.Dispose();
        }

        private void SendToParent(IpcMessage obj)
        {
            if (!IsDebugging)
                Console.WriteLine(JsonConvert.SerializeObject(obj));

            Debug.WriteLine(JsonConvert.SerializeObject(obj));
        }

        float MapWithPivot(
            float value,
            float sourceMin, 
            float sourceMax,
            float sourcePivot, 
            float targetMin, 
            float targetMax,
            float targetPivot)
        {
            // Clamp so we don't extrapolate
            if (value < sourceMin) 
                value = sourceMin;
            else if (value > sourceMax) 
                value = sourceMax;

            if (value >= sourcePivot)
            {
                // Upper half
                return targetPivot + (value - sourcePivot) /
                       (sourceMax - sourcePivot) * (targetMax - targetPivot);
            }
            else
            {
                // Lower half
                return targetPivot + (value - sourcePivot) /
                       (sourceMin - sourcePivot) * (targetMin - targetPivot);
            }
        }
    }
}
