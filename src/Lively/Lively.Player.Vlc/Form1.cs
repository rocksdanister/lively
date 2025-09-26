using CommandLine;
using LibVLCSharp.Shared;
using Lively.Common.Extensions;
using Lively.Common.Helpers;
using Lively.Common.JsonConverters;
using Lively.Models.Message;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Lively.Player.Vlc
{
    public partial class Form1 : Form
    {
        private LibVLC libVLC;
        private StartArgs startArgs;
        private MediaPlayer mediaPlayer;

        private bool IsDebugging { get; } = BuildInfoUtil.IsDebugBuild();

        public Form1()
        {
            InitializeComponent();
            if (IsDebugging)
            {
                startArgs = new StartArgs()
                {
                    FilePath = "http://commondatastorage.googleapis.com/gtv-videos-bucket/sample/BigBuckBunny.mp4",
                    Properties = @"",
                    Volume = 100
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
            }

            try
            {
                InitializeVLC();
            }
            catch (Exception ex)
            {
                ex.SendError(SendToParent, "Failed to initialize libVLC");
                // Exit or display custom error page.
                Environment.Exit(1);
            }
            finally
            {
                _ = ListenToParent();
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

        private void InitializeVLC()
        {
            Core.Initialize();

            libVLC = new LibVLC();
            mediaPlayer = new MediaPlayer(libVLC)
            {
                Volume = startArgs.Volume
            };
            videoView1.MediaPlayer = mediaPlayer;
            Load += Form1_Load;
            FormClosed += Form1_FormClosed;
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            var media = new Media(libVLC, new Uri(startArgs.FilePath));
            mediaPlayer?.Play(media);
            media.Dispose();
        }

        private void Form1_FormClosed(object sender, FormClosedEventArgs e)
        {
            mediaPlayer?.Stop();
            mediaPlayer?.Dispose();
            libVLC?.Dispose();
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
                                this.Invoke((Action)(() =>
                                {
                                    switch (obj.Type)
                                    {
                                        // TODO
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

        private void SendToParent(IpcMessage obj)
        {
            if (!IsDebugging)
                Console.WriteLine(JsonConvert.SerializeObject(obj));

            Debug.WriteLine(JsonConvert.SerializeObject(obj));
        }
    }
}
