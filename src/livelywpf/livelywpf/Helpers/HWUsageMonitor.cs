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
        /// Cpu usage % similar to taskmanager (Processor Time.)
        /// </summary>
        public float CPU { get; set; }
        /// <summary>
        /// Gpu usage % similar to taskmanager (GPU 3D Engine.)
        /// </summary>
        public float GPU  { get; set; }
        /// <summary>
        /// Free memory.
        /// </summary>
        public float RAM { get; set; }
    }

    // Todo:
    // Make start and stop service
    // Add more hardware - network down & up..
    // Add hardware model name (i5 4670k, gtx 1080..) to HWUsageMonitorEventArgs
    // Optimise idle usage, maybe shut it down when fullscreen app running?
    public sealed class HWUsageMonitor
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
        private static readonly HWUsageMonitor instance = new HWUsageMonitor();
        public event EventHandler<HWUsageMonitorEventArgs> HWMonitor = delegate { };
        private CancellationTokenSource ctsHwMonitor;

        //counter variables
        private PerformanceCounter cpuCounter = null;
        private PerformanceCounter ramCounter = null;

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
            HWMonitorService();
        }

        public void StartService()
        {
            throw new NotImplementedException();
        }

        public void StopService()
        {
            throw new NotImplementedException();
        }

        private void InitCounters()
        {
            try
            {
                cpuCounter = new PerformanceCounter
                    ("Processor", "% Processor Time", "_Total");

                ramCounter = new PerformanceCounter
                    ("Memory", "Available MBytes");
            }
            catch (Exception ex)
            {
                Logger.Error("PerfCounter: Init fail=>" + ex.Message);
            }
        }

        private async void HWMonitorService()
        {
            HWUsageMonitorEventArgs perfData = new HWUsageMonitorEventArgs();
            await Task.Run(async () =>
            {
                while(true)
                {
                    try
                    {
                        ctsHwMonitor.Token.ThrowIfCancellationRequested();
                        perfData.CPU = perfData.GPU = perfData.RAM = 0;
                        perfData.CPU = cpuCounter.NextValue();
                        perfData.RAM = ramCounter.NextValue();
                        //min 1sec wait required since some timers use pervious value for calculation.
                        //ref: https://docs.microsoft.com/en-us/archive/blogs/bclteam/how-to-read-performance-counters-ryan-byington
                        perfData.GPU = await GetGPUUsage();
                    }
                    catch (OperationCanceledException)
                    {
                        Logger.Info("PerfCounter: Stopped");
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
    }
}
