using System;
using CommandLine;
using Lively.Grpc.Client;
using System.Collections.Generic;
using Lively.Common.Helpers;
using Lively.Common.Helpers.Pinvoke;
using static Lively.Common.Constants;
using static Lively.Common.AutomationArgs;

namespace Lively.Commandline
{
    class Program
    {
        static void Main(string[] args)
        {
            _ = CommandLine.Parser.Default.ParseArguments<AppOptions, SetWallpaperOptions, CustomiseWallpaperOptions, CloseWallpaperOptions, ScreenSaverOptions, SeekWallpaperOptions, ScreenshotOptions>(args)
             .MapResult(
                 (AppOptions opts) => RunAppOptions(opts),
                 (SetWallpaperOptions opts) => RunSetWallpaperOptions(opts),
                 (CloseWallpaperOptions opts) => RunCloseWallpaperOptions(opts),
                 (SeekWallpaperOptions opts) => RunSeekWallpaperOptions(opts),
                 (CustomiseWallpaperOptions opts) => RunCustomiseWallpaperOptions(opts),
                 (ScreenSaverOptions opts) => RunScreenSaverOptions(opts),
                 (ScreenshotOptions opts) => RunScreenshotOptions(opts),
                 errs => HandleParseError(errs));


            if (!SingleInstanceUtil.IsAppMutexRunning(SingleInstance.UniqueAppName))
            {
                Console.WriteLine("\nWARNING: Lively core is currently not running!");
            }
            else
            {
                ICommandsClient commandsClient = new CommandsClient();
                commandsClient.AutomationCommand(args);
            }
        }

        //Empty just for initializing CommandlineParser --help docs
        private static object RunAppOptions(AppOptions opts)
        {
            return 0;
        }

        private static object RunSetWallpaperOptions(SetWallpaperOptions opts)
        {
            return 0;
        }

        private static object RunCloseWallpaperOptions(CloseWallpaperOptions opts)
        {
            return 0;
        }

        private static object RunSeekWallpaperOptions(SeekWallpaperOptions opts)
        {
            return 0;
        }

        private static object RunCustomiseWallpaperOptions(CustomiseWallpaperOptions opts)
        {
            return 0;
        }

        private static object RunScreenSaverOptions(ScreenSaverOptions opts)
        {
            return 0;
        }

        private static object RunScreenshotOptions(ScreenshotOptions opts)
        {
            return 0;
        }

        private static object HandleParseError(IEnumerable<Error> errs)
        {
            return 0;
        }
    }
}
