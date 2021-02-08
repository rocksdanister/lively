using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace livelySubProcess
{
    /// <summary>
    /// Kills external application type wallpapers in the event lively main pgm is killed by taskmanager/other pgms like av software.
    /// This is just incase safety, when shutdown properly the "wpItems" list is cleared by lively before exit.
    /// The external lively pgms such as livelycefsharp and libmpvplayer etc will close themselves if lively exits without subprocess.
    /// </summary>
    class Program
    {
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        public static extern Int32 SystemParametersInfo(UInt32 uiAction, UInt32 uiParam, String pvParam, UInt32 fWinIni);
        public static UInt32 SPI_SETDESKWALLPAPER = 20;
        public static UInt32 SPIF_UPDATEINIFILE = 0x1;

        readonly static List<int> wpItems = new List<int>();
        static void Main(string[] args)
        {
            int livelyId;
            Process lively;
            if (args.Length == 1)
            {
                try
                {
                    livelyId = Convert.ToInt32(args[0], 10);
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
                lively = Process.GetProcessById(livelyId);
            }
            catch
            {
                //"getting processname failure, ignoring"
                return;
            }
            ListenToParent();
            lively.WaitForExit();

            foreach (var item in wpItems)
            {
                try
                {
                    Process.GetProcessById(item).Kill();
                }
                catch { }
            }

            //force refresh desktop.
            SystemParametersInfo(SPI_SETDESKWALLPAPER, 0, null, SPIF_UPDATEINIFILE);
        }

        /// <summary>
        /// std I/O redirect, used to communicate with lively. 
        /// </summary>
        public static async void ListenToParent()
        {
            try
            {
                await Task.Run(async () =>
                {
                    while (true) // Loop runs only once per line received
                    {
                        string text = await Console.In.ReadLineAsync();
                        if (text.Equals("lively:clear", StringComparison.OrdinalIgnoreCase))
                        {
                            wpItems.Clear();
                        }
                        else if (Contains(text, "lively:add-pgm", StringComparison.OrdinalIgnoreCase))
                        {
                            var msg = text.Split(' ');
                            if (int.TryParse(msg[1], out int value))
                            {
                                wpItems.Add(value);
                            }
                        }
                        else if (Contains(text, "lively:rmv-pgm", StringComparison.OrdinalIgnoreCase))
                        {
                            var msg = text.Split(' ');
                            if (int.TryParse(msg[1], out int value))
                            {
                                wpItems.Remove(value);
                            }
                        }
                    }
                });
            }
            catch { }
        }

        /// <summary>
        /// String Contains method with StringComparison property.
        /// </summary>
        /// <param name="str"></param>
        /// <param name="substring"></param>
        /// <param name="comp"></param>
        /// <returns></returns>
        public static bool Contains(String str, String substring,
                                    StringComparison comp)
        {
            if (substring == null | str == null)
                throw new ArgumentNullException("string",
                                             "substring/string cannot be null.");
            else if (!Enum.IsDefined(typeof(StringComparison), comp))
                throw new ArgumentException("comp is not a member of StringComparison",
                                         "comp");

            return str.IndexOf(substring, comp) >= 0;
        }
    }
}
