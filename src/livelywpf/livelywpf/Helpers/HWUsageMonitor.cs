using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace livelywpf.Helpers
{
    public class HWUsageMonitorEventArgs : EventArgs
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

    // Todo:
    // Add more hardware.
    // Optimise idle usage, maybe shut it down when fullscreen app running?
    public sealed class HWUsageMonitor
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
        private static readonly HWUsageMonitor instance = new HWUsageMonitor();
        public event EventHandler<HWUsageMonitorEventArgs> HWMonitor = delegate { };
        private CancellationTokenSource ctsHwMonitor;
        private readonly HWUsageMonitorEventArgs perfData = new HWUsageMonitorEventArgs();

        //counter variables
        private PerformanceCounter cpuCounter = null;
        private PerformanceCounter ramCounter = null;
        private PerformanceCounter netDownCounter = null;
        private PerformanceCounter netUpCounter = null;

        public static HWUsageMonitor Instance
        {
            get
            {
                return instance;
            }
        }

        private HWUsageMonitor()
        {
            InitCounters();
        }

        public void StartService()
        {
            if(ctsHwMonitor == null)
            {
                ctsHwMonitor = new CancellationTokenSource();
                HWMonitorService();
            }
        }

        public void StopService()
        {
            if(ctsHwMonitor != null)
            {
                ctsHwMonitor.Cancel();
            }
        }

        private void InitCounters()
        {
            try
            {
                //hw info
                perfData.NameCpu = SystemInfo.GetCpu()[0];
                perfData.NameGpu = SystemInfo.GetGpu()[0];
                perfData.TotalRam = SystemInfo.GetTotalInstalledMemory();

                //counters
                cpuCounter = new PerformanceCounter
                    ("Processor", "% Processor Time", "_Total");

                ramCounter = new PerformanceCounter
                    ("Memory", "Available MBytes");

                var netCards = GetNetworkCards();
                if(netCards.Length != 0)
                {
                    //only considering the first card for now.
                    netDownCounter = new PerformanceCounter("Network Interface",
                                   "Bytes Received/sec", netCards[0]);

                    netUpCounter = new PerformanceCounter("Network Interface",
                                   "Bytes Sent/sec", netCards[0]);
                }
            }
            catch (Exception ex)
            {
                Logger.Error("PerfCounter: Init fail=>" + ex.Message);
            }
        }

        private async void HWMonitorService()
        {
            await Task.Run(async () =>
            {
                while(true)
                {
                    try
                    {
                        ctsHwMonitor.Token.ThrowIfCancellationRequested();
                        perfData.CurrentCpu = perfData.CurrentGpu3D = perfData.CurrentRamAvail = perfData.CurrentNetUp = perfData.CurrentNetDown = 0;
                        perfData.CurrentCpu = cpuCounter.NextValue();
                        perfData.CurrentRamAvail = ramCounter.NextValue();
                        perfData.CurrentNetDown = netDownCounter != null ? netDownCounter.NextValue() : 0f;
                        perfData.CurrentNetUp = netUpCounter != null ? netUpCounter.NextValue() : 0f;
                        //min 1sec wait required since some timers use pervious value for calculation.
                        //ref: https://docs.microsoft.com/en-us/archive/blogs/bclteam/how-to-read-performance-counters-ryan-byington
                        perfData.CurrentGpu3D = await GetGPUUsage();
                    }
                    catch (OperationCanceledException)
                    {
                        Logger.Info("PerfCounter: Cancelled");
                        ctsHwMonitor.Dispose();
                        ctsHwMonitor = null;
                        break;
                    }
                    catch 
                    {
                        //Logger.Error("PerfCounter: Timer fail=>" + ex.Message);
                    }
                    HWMonitor?.Invoke(this, perfData);
                }
            });
        }

        #region public helpers

        public static async Task<float> GetCPUUsage()
        {
            try
            {
                PerformanceCounter cpuCounter =
                    new PerformanceCounter("Processor", "% Processor Time", "_Total");
                _ = cpuCounter.NextValue();
                await Task.Delay(1000);
                return cpuCounter.NextValue();
            }
            catch
            {
                return 0f;
            }
        }

        public static float GetMemoryUsage()
        {
            try
            {
                PerformanceCounter memCounter =
                    new PerformanceCounter("Memory", "Available MBytes");
                return memCounter.NextValue();
            }
            catch
            {
                return 0f;
            }
        }

        //ref: https://stackoverflow.com/questions/56830434/c-sharp-get-total-usage-of-gpu-in-percentage
        public static async Task<float> GetGPUUsage()
        {
            try
            {
                var category = new PerformanceCounterCategory("GPU Engine");
                var counterNames = category.GetInstanceNames();
                var gpuCounters = new List<PerformanceCounter>();
                var result = 0f;

                foreach (string counterName in counterNames)
                {
                    if (counterName.EndsWith("engtype_3D"))
                    {
                        foreach (PerformanceCounter counter in category.GetCounters(counterName))
                        {
                            if (counter.CounterName == "Utilization Percentage")
                            {
                                gpuCounters.Add(counter);
                            }
                        }
                    }
                }

                gpuCounters.ForEach(x =>
                {
                    _ = x.NextValue();
                });
                await Task.Delay(1000);
                gpuCounters.ForEach(x =>
                {
                    result += x.NextValue();
                });

                return result;
            }
            catch
            {
                return 0f;
            }
        }

        public static async Task<Tuple<float, float>> GetNetworkUsage(string networkCard)
        {
            try
            {
                var netDown = new PerformanceCounter("Network Interface",
                                                   "Bytes Received/sec", networkCard);
                var netUp = new PerformanceCounter("Network Interface",
                                                        "Bytes Sent/sec", networkCard);
                _ = netDown.NextValue();
                _ = netUp.NextValue();
                await Task.Delay(1000);
                return Tuple.Create(netDown.NextValue(), netUp.NextValue());
            }
            catch 
            {
                return new Tuple<float, float>(0f, 0f);
            }
        }

        public static string[] GetNetworkCards()
        {
            try
            {
                var category = new PerformanceCounterCategory("Network Interface");
                return category.GetInstanceNames();
            }
            catch
            {
                return new string[] {};
            }
        }

        #endregion // public regions
    }
}
