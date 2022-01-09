using System;
using Lively.Grpc.Client;
using System.Threading.Tasks;
using System.ComponentModel;
using Lively.Models;

namespace Lively.ConsoleDemo
{
    class Program
    {
        static async Task Main(string[] args)
        {
            using IDesktopCoreClient coreClient = new WinDesktopCoreClient();
            IUserSettingsClient settingsClient = new UserSettingsClient();
            coreClient.WallpaperChanged += (s, e) => Console.WriteLine("\nWallpaper Changed Event");
            coreClient.DisplayChanged += (s, e) => Console.WriteLine("\nDisplay Changed Event.");

            bool showMenu = true;
            while (showMenu)
            {
                Console.WriteLine("\nChoose an option:");
                Console.WriteLine("1) Set Wallpaper");
                Console.WriteLine("2) Get Wallpapers");
                Console.WriteLine("3) Get Screens");
                Console.WriteLine("4) Close Wallpaper(s)");
                Console.WriteLine("5) Get Settings(s)");
                Console.WriteLine("6) Set Settings(s)");
                Console.WriteLine("9) Exit");
                Console.Write("\r\nSelect an option: ");

                switch (Console.ReadLine())
                {
                    case "1":
                        {
                            Console.WriteLine("\nEnter display id:");
                            var displayId = Console.ReadLine();
                            Console.WriteLine("\nEnter wallpaper metadata path:");
                            //Example: C:\Users\rocks\AppData\Local\Lively Wallpaper_v2\Library\wallpapers\iqdvd4pt.jyo
                            var path = Console.ReadLine();
                            await coreClient.SetWallpaper(path, displayId);
                        }
                        break;
                    case "2":
                        {
                            foreach (var item in coreClient.Wallpapers)
                            {
                                Console.WriteLine("GetWallpapers: " + item.LivelyInfoPath + " " + item.MonitorId);
                            }
                        }
                        break;
                    case "3":
                        {
                            foreach (var item in coreClient.DisplayMonitors)
                            {
                                Console.WriteLine("GetScreens: " + item.DeviceId + " " + item.Bounds);
                            }
                        }
                        break;
                    case "4":
                        {
                            Console.WriteLine("\nChoose an option:");
                            Console.WriteLine("1) Close all wallpaper(s)");
                            Console.WriteLine("2) Close wallpaper - monitor");
                            Console.WriteLine("3) Close wallpaper - library");
                            Console.WriteLine("4) Close wallpaper - category");
                            Console.WriteLine("5) Exit");
                            Console.Write("\r\nSelect an option: ");
                            switch (Console.ReadLine())
                            {
                                case "1":
                                    await coreClient.CloseAllWallpapers(true);
                                    break;
                                case "5":
                                    break;
                                default:
                                    throw new NotImplementedException();
                            }
                        }
                        break;
                    case "5":
                        {
                            PrintPropreties(settingsClient.Settings);
                        }
                        break;
                    case "6":
                        {
                            settingsClient.Settings.SysTrayIcon = !settingsClient.Settings.SysTrayIcon;
                            await settingsClient.Save<ISettingsModel>();
                        }
                        break;
                    case "9":
                        await coreClient.ShutDown();
                        Console.WriteLine("Core shut down complete..");
                        Console.ReadLine();
                        showMenu = false;
                        break;
                }
            }
        }

        public static void PrintPropreties(object obj)
        {
            foreach (PropertyDescriptor descriptor in TypeDescriptor.GetProperties(obj))
            {
                string name = descriptor.Name;
                object value = descriptor.GetValue(obj);
                Console.WriteLine("{0}={1}", name, value);
            }
        }
    }
}
