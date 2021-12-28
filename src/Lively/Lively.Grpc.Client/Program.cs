using Desktop;
using GrpcDotNetNamedPipes;
using Lively.Common;
using System;

namespace Lively.Grpc.Client
{
    class Program
    {
        static void Main(string[] args)
        {
            var client = new DesktopService.DesktopServiceClient(GetChannel());
            var request = new WallpaperRequest
            {
                LivelyInfoPath = @"C:\Users\rocks\AppData\Local\Lively Wallpaper_v2\Library\wallpapers\iqdvd4pt.jyo",
                MonitorId = "1",
            };

            var response = client.SetWallpaper(request);
            Console.WriteLine("SetWallpaper: " + response.Status);
        }

        private static NamedPipeChannel GetChannel() => 
            new NamedPipeChannel(".", Constants.SingleInstance.GrpcPipeServerName);
    }
}
