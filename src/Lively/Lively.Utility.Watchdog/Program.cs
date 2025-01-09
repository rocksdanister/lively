using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Lively.Utility.Watchdog
{
    /// <summary>
    /// Kills external application type wallpapers in the event lively main pgm is killed by taskmanager/other programs.
    /// <br>Commands:</br> 
    /// <br>ADD/RMV {pid} - Add/remove program from watchlist.</br>
    /// <br>CLR - Clear watchlist.</br>
    /// </summary>
    public class Program
    {
        private static readonly List<int> activePrograms = new List<int>();

        static void Main(string[] args)
        {
            int parentProcessId;
            Process parentProcess;

            if (args.Length == 1)
            {
                try
                {
                    parentProcessId = Convert.ToInt32(args[0], 10);
                }
                catch
                {
                    //ERROR: converting toint
                    return;
                }
            }
            else
            {
                //"Incorrent no of arguments."
                return;
            }

            try
            {
                parentProcess = Process.GetProcessById(parentProcessId);
            }
            catch
            {
                //getting processname failure!
                return;
            }

            StdInListener();
            parentProcess.WaitForExit();

            foreach (var item in activePrograms)
            {
                try
                {
                    Process.GetProcessById(item).Kill();
                }
                catch { }
            }

            foreach (var item in Process.GetProcessesByName("Lively.UI.WinUI"))
            {
                try
                {
                    item.Kill();
                }
                catch { }
            }

            //force refresh desktop.
            _ = SystemParametersInfo(SPI_SETDESKWALLPAPER, 0, null, SPIF_UPDATEINIFILE);
        }


        /// <summary>
        /// std I/O redirect.
        /// </summary>
        private static async void StdInListener()
        {
            try
            {
                await Task.Run(async () =>
                {
                    while (true)
                    {
                        var msg = await Console.In.ReadLineAsync();
                        var args = msg.Split(' ');
                        if (args[0].Equals("CLR", StringComparison.OrdinalIgnoreCase))
                        {
                            activePrograms.Clear();
                        }
                        else if (args[0].Equals("ADD", StringComparison.OrdinalIgnoreCase))
                        {
                            if (int.TryParse(args[1], out int value))
                            {
                                activePrograms.Add(value);
                            }
                        }
                        else if (args[0].Equals("RMV", StringComparison.OrdinalIgnoreCase))
                        {
                            if (int.TryParse(args[1], out int value))
                            {
                                _ = activePrograms.Remove(value);
                            }
                        }
                    }
                });
            }
            catch { }
        }

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        [return: MarshalAs(UnmanagedType.I4)]
        private static extern Int32 SystemParametersInfo(UInt32 uiAction, UInt32 uiParam, String pvParam, UInt32 fWinIni);
        private static UInt32 SPI_SETDESKWALLPAPER = 20;
        private static UInt32 SPIF_UPDATEINIFILE = 0x1;
    }
}
