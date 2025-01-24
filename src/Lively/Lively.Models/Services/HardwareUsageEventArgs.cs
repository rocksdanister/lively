using System;

namespace Lively.Models.Services
{
    public class HardwareUsageEventArgs : EventArgs
    {
        /// <summary>
        /// Primary cpu name.
        /// </summary>
        public string NameCpu { get; set; }
        /// <summary>
        /// Primary gpu name.
        /// </summary>
        public string NameGpu { get; set; }
        /// <summary>
        /// Cpu usage % similar to taskmanager (Processor Time.)
        /// </summary>
        public string NameNetCard { get; set; }
        /// <summary>
        /// Current total cpu usage %.
        /// </summary>
        public float CurrentCpu { get; set; }
        /// <summary>
        /// Gpu usage % similar to taskmanager (GPU 3D Engine.)
        /// </summary>
        public float CurrentGpu3D { get; set; }
        /// <summary>
        /// Free memory in Megabytes.
        /// </summary>
        public float CurrentRamAvail { get; set; }
        /// <summary>
        /// Network download speed (Bytes/Sec)
        /// </summary>
        public float CurrentNetDown { get; set; }
        /// <summary>
        /// Network upload speed (Bytes/Sec)
        /// </summary>
        public float CurrentNetUp { get; set; }
        /// <summary>
        /// Full system ram amount (MegaBytes)
        /// </summary>
        public long TotalRam { get; set; }
    }
}
