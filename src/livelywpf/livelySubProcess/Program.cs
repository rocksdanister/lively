using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace livelySubProcess
{
    /// <summary>
    /// Runs in the background, cleans up external wp pgms in the event lively crash.
    /// </summary>
    class Program
    {
        [DllImport("user32.dll", SetLastError = true)]
        public static extern uint GetWindowThreadProcessId(IntPtr hWnd, out int lpdwProcessId);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        public static extern Int32 SystemParametersInfo(UInt32 uiAction, UInt32 uiParam, String pvParam, UInt32 fWinIni);
        public static UInt32 SPI_SETDESKWALLPAPER = 20;
        public static UInt32 SPIF_UPDATEINIFILE = 0x1;
        /// <summary>
        /// portable lively build, no installer.
        /// </summary>
        public static readonly bool isPortableBuild = false;
        public static string PathData { get; private set; }

        static void Main(string[] args)
        {

            if (isPortableBuild)
            {
                PathData = AppDomain.CurrentDomain.BaseDirectory;
            }
            else
            {
                PathData = Path.Combine(System.Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Lively Wallpaper");
            }

            int livelyId;
            Process lively;
            if (args.Length == 0)
            {
                Console.WriteLine("NO arguments sent.");
                //Console.Read();
                return;
            }

            if (args.Length == 1)
            {
                try
                {
                    livelyId = Convert.ToInt32(args[0], 10);
                }
                catch
                {
                    Console.WriteLine("ERROR: converting toint");
                    //Console.Read();
                    return;
                }
            }
            else
            {
                Console.WriteLine("Incorrent no of arguments.");
                //Console.Read();
                return;
            }

            try
            {
                lively = Process.GetProcessById(livelyId);
            }
            catch
            {
                Console.WriteLine("getting processname failure, ignoring");
                //Console.Read();
                return;
            }

            if (!lively.ProcessName.Equals("livelywpf", StringComparison.InvariantCultureIgnoreCase))
            {
                Console.WriteLine("Error: Not livelywpf :- " + lively.ProcessName);
                //Console.Read();
                return;
            }

            lively.WaitForExit();

            Console.WriteLine("done waiting, ready to kill..");
            //Console.Read();

            FileHandle.LoadRunningPrograms();

            foreach (var proc in Process.GetProcesses())
            {
                Console.WriteLine("pgm list:- " + proc.ProcessName + " " + proc.MainWindowHandle);
                foreach (var wproc in FileHandle.runningPrograms)
                {
                    //Pid + Process-name is unique enough to make sure its the correct pgm.
                    if (proc.ProcessName.Equals(wproc.ProcessName, StringComparison.OrdinalIgnoreCase) && proc.Id == wproc.Pid)//&& IntPtr.Equals(proc.MainWindowHandle,wproc.handle))//proc.Handle == wproc.handle)
                    {   
                        Console.WriteLine("Unclosed pgm, kill:- " + proc.ProcessName);
                        try
                        {
                            proc.Kill();
                        }
                        catch { }

                    }
                }
            }

            //force refresh desktop.
            SystemParametersInfo(SPI_SETDESKWALLPAPER, 0, null, SPIF_UPDATEINIFILE);

            FileHandle.runningPrograms.Clear();
            FileHandle.SaveRunningPrograms();
        }
    }

    class FileHandle
    {
        [Serializable]
        public class RunningProgram
        {
            public string ProcessName { get; set; }
            public int Pid { get; set; }
            public RunningProgram()
            {
                Pid = 0;
                ProcessName = null;
            }
        }

        public static List<RunningProgram> runningPrograms = new List<RunningProgram>();
        //private static string pathData = Path.Combine(System.Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Lively Wallpaper");

        public class RunningProgramsList
        {
            public List<RunningProgram> Item { get; set; }
        }

        public static void LoadRunningPrograms()
        {
            try
            {
                // deserialize JSON directly from a file
                using (StreamReader file = File.OpenText( Path.Combine(Program.PathData, "lively_running_pgms.json")))
                {
                    JsonSerializer serializer = new JsonSerializer();
                    RunningProgramsList tmp = (RunningProgramsList)serializer.Deserialize(file, typeof(RunningProgramsList));
                    runningPrograms = tmp.Item;
                }
            }
            catch 
            {
                Debug.WriteLine("failed to read json");
            }
        }


        public static void SaveRunningPrograms()
        {
            RunningProgramsList tmp = new RunningProgramsList
            {
                Item = runningPrograms
            };

            JsonSerializer serializer = new JsonSerializer
            {
                Formatting = Formatting.Indented,

                //serializer.Converters.Add(new JavaScriptDateTimeConverter());
                NullValueHandling = NullValueHandling.Include
            };

            try
            {
                using (StreamWriter sw = new StreamWriter(Path.Combine(Program.PathData, "lively_running_pgms.json")))
                using (JsonWriter writer = new JsonTextWriter(sw))
                {
                    serializer.Serialize(writer, tmp);
                }
            }
            catch
            {
                Debug.WriteLine("failed to write json");
            }
        }

    }
}
