using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Lively.Core;
using Lively.Core.Display;

namespace Lively.Services
{
    internal class TimerService
    {
        public static TimerService Instance { get; private set; }
        // get the logger to debug
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        // basic variable for the repeating task
        private Task? timerTask;
        private PeriodicTimer timer;
        private CancellationTokenSource cts = new();

        private readonly IDisplayManager displayManager;
        private readonly IDesktopCore desktopCore;

        // to create a new Timer Service you must specify the inerval with a TimeSpan object 
        // for example : TimeSpan intervall = TimeSpan.FromMinutes(15);
        public TimerService(TimeSpan interval, IDisplayManager displayManager, IDesktopCore desktopCore)
        {
            this.displayManager = displayManager;
            this.desktopCore = desktopCore;
            timer = new PeriodicTimer(interval);
            Instance = this;
        }

        public void Start()
        {
            timerTask = RandomWallpaperCycle();
        }

        // this is where the program apply the new wallpaper
        private async Task RandomWallpaperCycle()
        {
            try
            {
                Logger.Info("Succesfully launched the random wallpaper cycle !"+timer.ToString());
                while (await timer.WaitForNextTickAsync(cts.Token))
                {
                    // set wallpaper here
                    desktopCore.SetRandomWallpaper();
                }
            }
            catch { }
        }

        public async Task Stop()
        {
            if(timerTask is null)
            {
                return;
            }
            cts.Cancel();
            await timerTask;
            cts.Dispose();
            Logger.Info("Task was cancelled");
        }

        public void ChangeTimerIntervall(TimeSpan intervall)
        {
            timer = new PeriodicTimer(intervall);
            cts = new();
        }

    }
}
