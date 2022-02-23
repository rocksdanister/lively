using Lively.Common.Helpers;
using Lively.Grpc.Client;
using System;
using static Lively.Common.Constants;

//Reference: https://sites.harding.edu/fmccown/screensaver/screensaver.html
//CC BY-SA 2.0
namespace Lively.Screensaver
{
    class Program
    {
        static void Main(string[] args)
        {
            if (!SingleInstanceUtil.IsAppMutexRunning(SingleInstance.UniqueAppName))
            {
                return;
            }

            ICommandsClient commandsClient = new CommandsClient();
            if (args.Length > 0)
            {
                string firstArgument = args[0].ToLower().Trim();
                string secondArgument = null;

                // Handle cases where arguments are separated by colon.
                // Examples: /c:1234567 or /P:1234567
                if (firstArgument.Length > 2)
                {
                    secondArgument = firstArgument.Substring(3).Trim();
                    firstArgument = firstArgument.Substring(0, 2);
                }
                else if (args.Length > 1)
                    secondArgument = args[1];

                if (firstArgument == "/c")  // Configuration mode
                {
                    _ = commandsClient.ScreensaverConfigure();
                }
                else if (firstArgument == "/p") // Preview mode
                {
                    _ = commandsClient.ScreensaverPreview(Int32.Parse(secondArgument));
                }
                else if (firstArgument == "/s") // Full-screen mode
                {
                    _ = commandsClient.ScreensaverShow(true);
                }
                else { }  // Undefined argument
            }
            else  // No arguments - treat like /c
            {
                _ = commandsClient.ScreensaverConfigure();
            }
        }
    }
}
