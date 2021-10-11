using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

namespace livelywpf.Helpers.Scheduler
{
    public enum SchedulerEventType
    {
        [Description("Time based wallpaper.")]
        time,
        [Description("Random wallpaper.")]
        shuffle,
        [Description("Power plugged/unplugged.")]
        acpi
    }

    public interface IScheduler
    {
        /// <summary>
        /// Event type.
        /// </summary>
        /// <returns></returns>
        SchedulerEventType GetEventType();
        /// <summary>
        /// Check if event is ready to fire.
        /// </summary>
        /// <returns></returns>
        bool IsReady();
        /// <summary>
        /// Livelynfo.json path.
        /// </summary>
        /// <returns></returns>
        string GetWallpaperPath();
    }
}
