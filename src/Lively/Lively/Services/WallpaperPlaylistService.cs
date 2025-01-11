using Lively.Common.Services;
using Lively.Core;
using Lively.Core.Display;
using System.Timers;

namespace Lively.Services
{
    public class WallpaperPlaylistService
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
        private readonly Timer idleTimer = new Timer();

        private readonly IUserSettingsService userSettings;
        private readonly IDesktopCore desktopCore;
        private readonly IDisplayManager displayManager;

        public WallpaperPlaylistService(IUserSettingsService userSettings,
            IDisplayManager displayManager,
            IDesktopCore desktopCore)
        {
            this.userSettings = userSettings;
            this.desktopCore = desktopCore;
            this.displayManager = displayManager;

            idleTimer.Elapsed += IdleCheckTimer;
            idleTimer.Interval = 30000;
        }

        public bool IsRunning { get; private set; } = false;

        private void IdleCheckTimer(object sender, ElapsedEventArgs e)
        {
            
        }
    }
}
