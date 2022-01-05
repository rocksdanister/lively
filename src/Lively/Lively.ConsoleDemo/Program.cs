using System;
using Lively.Grpc.Client;
using System.Threading.Tasks;

namespace Lively.ConsoleDemo
{
    class Program
    {
        static async Task Main(string[] args)
        {
            using var client = new WinDesktopCoreClient();
            client.WallpaperChanged += (s, e) => Console.WriteLine("\nWallpaper Changed Event");
            client.DisplayChanged += (s, e) => Console.WriteLine("\nDisplay Changed Event.");

            bool showMenu = true;
            while (showMenu)
            {
                Console.WriteLine("\nChoose an option:");
                Console.WriteLine("1) Set Wallpaper");
                Console.WriteLine("2) Get Wallpapers");
                Console.WriteLine("3) Get Screens");
                Console.WriteLine("4) Close Wallpaper(s)");
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
                            await client.SetWallpaper(path, displayId);
                        }
                        break;
                    case "2":
                        {
                            foreach (var item in client.Wallpapers)
                            {
                                Console.WriteLine("GetWallpapers: " + item.LivelyInfoPath + " " + item.MonitorId);
                            }
                        }
                        break;
                    case "3":
                        {
                            foreach (var item in client.DisplayMonitors)
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
                                    await client.CloseAllWallpapers(true);
                                    break;
                                case "5":
                                    break;
                                default:
                                    throw new NotImplementedException();
                            }
                        }
                        break;
                    case "9":
                        await client.ShutDown();
                        Console.WriteLine("Core shut down complete..");
                        Console.ReadLine();
                        showMenu = false;
                        break;
                }
            }
        }
    }
}
