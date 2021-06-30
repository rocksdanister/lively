using System;

namespace livelyScreenSaver
{
    class Program
    {
        private static readonly string uniqueAppName = "LIVELY:DESKTOPWALLPAPERSYSTEM";
        private static readonly string pipeServerName = uniqueAppName + Environment.UserName;

        static void Main(string[] args)
        {
            string[] msg = null;
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

                if (firstArgument == "/c")           // Configuration mode
                {
                    msg = new string[] { "screensaver", "--configure", "0" };
                }
                else if (firstArgument == "/p")      // Preview mode
                {
                    msg = new string[] { "screensaver", "--preview", secondArgument };
                }
                else if (firstArgument == "/s")      // Full-screen mode
                {
                    msg = new string[] { "screensaver", "--show", "true" };
                }
                else    // Undefined argument
                {

                }
            }
            else    // No arguments - treat like /c
            {
                msg = new string[] { "screensaver", "--configure", "0" };
            }


            try
            {
                if (msg != null)
                {
                    livelywpf.Helpers.PipeClient.SendMessage(pipeServerName, msg);
                }
            }
            catch { }
        }
    }
}
