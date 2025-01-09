using Lively.Models;
using System.Diagnostics;

namespace Lively.Common.Factories
{
    public interface IApplicationsFactory
    {
        ApplicationModel CreateApp(Process process);
        ApplicationModel CreateApp(string path);
    }
}