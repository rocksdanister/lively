using CommandLine;

namespace Lively.UI.WinUI
{
    public class StartArgs
    {
        [Option("trayWidget",
        Required = false,
        HelpText = "Run customise-traymenu without initializing MainWindow.")]
        public bool TrayWidget { get; set; }

        [Option("appUpdate",
        Required = false,
        HelpText = "Open update page.")]
        public bool AppUpdate { get; set; }
    }
}
