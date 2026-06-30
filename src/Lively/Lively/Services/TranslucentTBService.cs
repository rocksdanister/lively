using ImageMagick;
using Lively.Common.Services;
using Lively.Models.Enums;
using Lively.Services.Taskbar;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Windows;

namespace Lively.Services
{
    public class TranslucentTBService : ITransparentTbService
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
        private readonly static IDictionary<string, string> IncompatiblePrograms = new Dictionary<string, string>() {
            {"TranslucentTB", "344635E9-9AE4-4E60-B128-D53E25AB70A7"},
            {"TaskbarX", null}, // Program does not publish a mutex.
        };

        private readonly ITaskbarThemeBackend backend;
        private readonly bool incompatibleProgramFound;
        private bool disposedValue;
        private Color accentColor = Color.FromArgb(0, 0, 0);
        private TaskbarTheme taskbarTheme = TaskbarTheme.none;

        public TranslucentTBService()
        {
            if (CheckIncompatiblePrograms() is string pgm)
            {
                Logger.Info($"Transparent taskbar disabled, incompatible program found: {pgm}");
                incompatibleProgramFound = true;
            }

            backend = TaskbarThemeBackendFactory.Create();
            SystemEvents.SessionSwitch += SystemEvents_SessionSwitch;
            SystemEvents.UserPreferenceChanged += SystemEvents_UserPreferenceChanged;
        }

        public bool IsRunning => backend.IsRunning;

        public string CheckIncompatiblePrograms()
        {
            foreach (var item in IncompatiblePrograms)
            {
                if (item.Value != null)
                {
                    try
                    {
                        Mutex mutex = null;
                        try
                        {
                            if (Mutex.TryOpenExisting(item.Value, out mutex))
                            {
                                return item.Key;
                            }
                        }
                        finally
                        {
                            mutex?.Dispose();
                        }
                    }
                    catch { } // Skipping best-effort process compatibility detection.
                }
                else
                {
                    try
                    {
                        var proc = Process.GetProcessesByName(item.Key);
                        if (proc.Count() != 0)
                        {
                            return item.Key;
                        }
                    }
                    catch { } // Skipping best-effort process compatibility detection.
                }
            }
            return null;
        }

        public Color GetAverageColor(string imgPath)
        {
            using var image = new MagickImage(imgPath);
            image.Scale(1, 1);

            using var pixels = image.GetPixels();
            var color = pixels.GetPixel(0, 0).ToColor();

            return Color.FromArgb(255 * color.R / 255, 255 * color.G / 255, 255 * color.B / 255);
        }

        public void SetAccentColor(Color color)
        {
            accentColor = color;
            Start(taskbarTheme);
        }

        public void Refresh()
        {
            if (incompatibleProgramFound || taskbarTheme == TaskbarTheme.none)
                return;

            if (SystemParameters.HighContrast)
            {
                Stop();
                return;
            }

            backend.Refresh();
        }

        public void Start(TaskbarTheme theme)
        {
            taskbarTheme = theme;
            if (incompatibleProgramFound)
                return;

            if (SystemParameters.HighContrast)
            {
                Stop();
                return;
            }

            if (theme == TaskbarTheme.none)
            {
                Stop();
                return;
            }

            backend.Start(new TaskbarThemeState(theme, accentColor));
            if (backend.IsRunning)
            {
                Logger.Info("Taskbar theme service started using {0}.", backend.Name);
            }
        }

        public void Stop()
        {
            var wasRunning = backend.IsRunning;
            backend.Stop();
            if (wasRunning)
            {
                Logger.Info("Taskbar theme service stopped.");
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    SystemEvents.SessionSwitch -= SystemEvents_SessionSwitch;
                    SystemEvents.UserPreferenceChanged -= SystemEvents_UserPreferenceChanged;
                    backend.Dispose();
                }

                disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        private void SystemEvents_SessionSwitch(object sender, SessionSwitchEventArgs e)
        {
            if (e.Reason == SessionSwitchReason.SessionUnlock && taskbarTheme != TaskbarTheme.none)
            {
                Refresh();
            }
        }

        private void SystemEvents_UserPreferenceChanged(object sender, UserPreferenceChangedEventArgs e)
        {
            if (e.Category != UserPreferenceCategory.Accessibility)
                return;

            if (SystemParameters.HighContrast)
            {
                Stop();
                return;
            }

            if (taskbarTheme != TaskbarTheme.none)
            {
                Start(taskbarTheme);
            }
        }
    }
}
