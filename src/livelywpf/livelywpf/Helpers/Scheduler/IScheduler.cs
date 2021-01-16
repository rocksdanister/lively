using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

namespace livelywpf.Helpers
{
    public enum ShedulerEventType
    {
        [Description("Time based wallpaper.")]
        time,
        [Description("Random wallpaper.")]
        shuffle,
        [Description("Power plugged/unplugged.")]
        acpi
    }

    interface IScheduler
    {
        /// <summary>
        /// Event type.
        /// </summary>
        /// <returns></returns>
        public ShedulerEventType GetEventType();
        /// <summary>
        /// Check if event is ready to fire.
        /// </summary>
        /// <returns></returns>
        public bool IsReady();
    }
}
