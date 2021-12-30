using System;
using System.Threading.Tasks;

namespace Lively.Grpc.Client
{
    class Program
    {
        static async Task Main(string[] args)
        {
            using var client = new WinDesktopCoreClient();
            client.WallpaperChanged += (s, e) => Console.WriteLine("\nWallpaper Changed Event: " + e);

            bool showMenu = true;
            while (showMenu)
            {
                //Console.Clear();
                Console.WriteLine("\nChoose an option:");
                Console.WriteLine("1) Set Wallpaper");
                Console.WriteLine("2) Get Wallpapers");
                Console.WriteLine("3) Get Screens");
                Console.WriteLine("9) Exit");
                Console.Write("\r\nSelect an option: ");

                switch (Console.ReadLine())
                {
                    case "1":
                        {
                            Console.WriteLine("\nEnter display id:");
                            var displayId = Console.ReadLine();
                            var status = await client.SetWallpaper(@"C:\Users\rocks\AppData\Local\Lively Wallpaper_v2\Library\wallpapers\iqdvd4pt.jyo", displayId);
                            Console.WriteLine("SetWallpaper: " + status);
                        }
                        break;
                    case "2":
                        {
                            foreach (var item in await client.GetWallpapers())
                            {
                                Console.WriteLine("GetWallpapers: " + item.LivelyInfoPath + " " + item.MonitorId);
                            }
                        }
                        break;
                    case "3":
                        {
                            foreach (var item in await client.GetScreens())
                            {
                                Console.WriteLine("GetScreens: " + item.DeviceId);
                            }
                        }
                        break;
                    case "9":
                        showMenu = false;
                        break;
                }
            }
        }
    }
}
