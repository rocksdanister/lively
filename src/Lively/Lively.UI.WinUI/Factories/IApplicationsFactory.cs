using Lively.Models;
using System.Diagnostics;

namespace Lively.UI.WinUI.Factories
{
    public interface IApplicationsFactory
    {
        ApplicationModel CreateApp(Process process);
        ApplicationModel CreateApp(string path);
    }
}