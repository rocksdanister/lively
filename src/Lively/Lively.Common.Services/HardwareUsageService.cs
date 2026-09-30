using Lively.Helpers.Hardware;
using Lively.Models.Services;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace Lively.Common.Services
{
    public class HardwareUsageService : IHardwareUsageService
    {
        public event EventHandler<HardwareUsageEventArgs> HWMonitor = delegate { };
        private readonly HardwareUsageEventArgs perfData = new HardwareUsageEventArgs();
        private CancellationTokenSource ctsHwMonitor;

        //counter variables
        private PerformanceCounter cpuCounter = null;
        private PerformanceCounter ramCounter = null;
        private PerformanceCounter netDownCounter = null;
        private PerformanceCounter netUpCounter = null;

        public HardwareUsageService()
        {
            InitializePerfCounters();
        }

        public void Start()
        {
            if (ctsHwMonitor == null)
            {
                ctsHwMonitor = new CancellationTokenSource();
                HWMonitorLoop();
            }
            else
            {
                throw new InvalidOperationException("Service once stopped cannot be restarted!");
            }
        }

        public void Stop()
        {
            ctsHwMonitor?.Cancel();
        }

        private void InitializePerfCounters()
        {
            try
            {
                //hw info
                perfData.NameCpu = SystemInfo.GetCpu().Count != 0 ? SystemInfo.GetCpu()[0] : null;
                perfData.NameGpu = SystemInfo.GetGpu().Count != 0 ? SystemInfo.GetGpu()[0] : null;
                perfData.NameNetCard = GetPrimaryNetworkCard();
                perfData.TotalRam = SystemInfo.GetTotalInstalledMemory();

                //counters
                cpuCounter = new PerformanceCounter
                    ("Processor", "% Processor Time", "_Total");

                ramCounter = new PerformanceCounter
                    ("Memory", "Available MBytes");

                if (perfData.NameNetCard != null)
                {
                    netDownCounter = new PerformanceCounter("Network Interface",
                                   "Bytes Received/sec", perfData.NameNetCard);

                    netUpCounter = new PerformanceCounter("Network Interface",
                                   "Bytes Sent/sec", perfData.NameNetCard);
                }
            }
            catch
            {
                //TODO
            }
        }

        private async void HWMonitorLoop()
        {
            try
            {
                await Task.Run(async () =>
                {
                    while (true)
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
                            ctsHwMonitor.Dispose();
                            //ctsHwMonitor = null;
                            break;
                        }
                        catch
                        {
                            //todo: log error.
                        }
                        HWMonitor?.Invoke(this, perfData);
                    }
                });
            }
            finally
            {
                cpuCounter?.Dispose();
                ramCounter?.Dispose();
                netDownCounter?.Dispose();
                netUpCounter?.Dispose();
            }
        }

        #region public helpers

        public static async Task<float> GetCPUUsage()
        {
            try
            {
                using (PerformanceCounter cpuCounter =
                    new PerformanceCounter("Processor", "% Processor Time", "_Total"))
                {
                    _ = cpuCounter.NextValue();
                    await Task.Delay(1000);
                    return cpuCounter.NextValue();
                }
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
                using (PerformanceCounter memCounter =
                    new PerformanceCounter("Memory", "Available MBytes"))
                {
                    return memCounter.NextValue();
                }
            }
            catch
            {
                return 0f;
            }
        }

        //ref: https://stackoverflow.com/questions/56830434/c-sharp-get-total-usage-of-gpu-in-percentage
        public static async Task<float> GetGPUUsage()
        {
            var gpuCounters = new List<PerformanceCounter>();
            var result = 0f;
            try
            {
                var category = new PerformanceCounterCategory("GPU Engine");
                var counterNames = category.GetInstanceNames();

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
            }
            catch
            {
                result = 0f;
            }
            finally
            {
                gpuCounters.ForEach(x =>
                {
                    x?.Dispose();
                });
            }
            return result;
        }

        public static async Task<Tuple<float, float>> GetNetworkUsage(string networkCard)
        {
            try
            {
                using (var netDown = new PerformanceCounter("Network Interface",
                                                   "Bytes Received/sec", networkCard))
                {
                    using (var netUp = new PerformanceCounter("Network Interface",
                                                        "Bytes Sent/sec", networkCard))
                    {
                        _ = netDown.NextValue();
                        _ = netUp.NextValue();
                        await Task.Delay(1000);
                        return Tuple.Create(netDown.NextValue(), netUp.NextValue());
                    }
                }
            }
            catch
            {
                return Tuple.Create(0f, 0f);
            }
        }

        public static List<string> GetNetworkCards()
        {
            var result = new List<string>();
            try
            {
                var category = new PerformanceCounterCategory("Network Interface");
                result.AddRange(category.GetInstanceNames());
            }
            catch { }
            return result;
        }

        /// <summary>
        /// Network interface that is actually connected, null if there is none.
        /// </summary>
        /// <remarks>
        /// The first instance is not necessarily a usable adapter. Disconnected cards are
        /// listed as well, and an adapter whose description contains '#' (Windows appends
        /// it to disambiguate duplicates) additionally produces a phantom base instance,
        /// because '#' is the separator PDH itself uses for duplicate instance names.
        /// Both always report zero, so pick by link speed instead of by position.
        /// </remarks>
        public static string GetPrimaryNetworkCard()
        {
            var cards = GetNetworkCards();
            string primary = null;
            var maxBandwidth = 0f;

            foreach (var card in cards)
            {
                try
                {
                    using (var bandwidth = new PerformanceCounter("Network Interface",
                                       "Current Bandwidth", card))
                    {
                        var value = bandwidth.NextValue();
                        if (value > maxBandwidth)
                        {
                            maxBandwidth = value;
                            primary = card;
                        }
                    }
                }
                catch { }
            }

            //no counter responded, keep the previous behaviour rather than showing nothing.
            return primary ?? (cards.Count != 0 ? cards[0] : null);
        }

        #endregion // public regions
    }
}
