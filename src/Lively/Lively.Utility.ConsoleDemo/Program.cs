using Lively.Common.Helpers;
using Lively.Grpc.Client;
using Lively.Models;
using System;
using System.Threading.Tasks;

namespace Lively.Utility.ConsoleDemo
{
    class Program
    {
        static async Task Main(string[] args)
        {
            using IDesktopCoreClient coreClient = new WinDesktopCoreClient();
            using IDisplayManagerClient displayManager = new DisplayManagerClient();
            IUserSettingsClient settingsClient = new UserSettingsClient();
            ICommandsClient commandsClient = new CommandsClient();
            coreClient.WallpaperChanged += (s, e) => Console.WriteLine("\nWallpaper Changed Event");
            coreClient.WallpaperError += (s, e) => Console.WriteLine(e.ToString());
            displayManager.DisplayChanged += (s, e) => Console.WriteLine("\nDisplay Changed Event.");

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
                Console.WriteLine("7) Start screensaver(s)");
                Console.WriteLine("8) Commandline control");
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
                                Console.WriteLine("GetWallpaper() " + LogUtil.PropertyList(item));
                            }
                        }
                        break;
                    case "3":
                        {
                            foreach (var item in displayManager.DisplayMonitors)
                            {
                                Console.WriteLine("GetScreens() " + LogUtil.PropertyList(item));
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
                            Console.WriteLine(LogUtil.PropertyList(settingsClient.Settings));
                        }
                        break;
                    case "6":
                        {
                            settingsClient.Settings.SysTrayIcon = !settingsClient.Settings.SysTrayIcon;
                            await settingsClient.SaveAsync<SettingsModel>();
                        }
                        break;
                    case "7":
                        {
                            Console.Write("Please wait..");
                            await Task.Delay(1000); //delay because "Enter" key triggering screensaver exit.
                            await commandsClient.ScreensaverShow(true);
                        }
                        break;
                    case "8":
                        {
                            Console.Write("Enter commandline command:");
                            var msg = Console.ReadLine();
                            var arguments = msg.Split(" ");
                            await commandsClient.AutomationCommandAsync(arguments);
                        }
                        break;
                    case "9":
                        await commandsClient.ShutDown();
                        Console.WriteLine("Core shut down complete..");
                        Console.ReadLine();
                        showMenu = false;
                        break;
                }
            }
        }
    }
}
