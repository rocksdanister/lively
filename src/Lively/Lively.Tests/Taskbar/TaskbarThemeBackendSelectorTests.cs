using Lively.Services.Taskbar;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace Lively.Tests.Taskbar
{
    [TestClass]
    public sealed class TaskbarThemeBackendSelectorTests
    {
        [TestMethod]
        [DataRow("10.0.19045.0", false)]
        [DataRow("10.0.19045.0", true)]
        [DataRow("10.0.22000.0", false)]
        [DataRow("10.0.22621.0", true)]
        [DataRow("10.0.22621.1342", true)]
        public void SelectUsesLegacyBackendBeforeModernWindows11Cutoff(string version, bool utilityAvailable)
        {
            var selected = TaskbarThemeBackendSelector.Select(Version.Parse(version), utilityAvailable);

            Assert.AreEqual(TaskbarThemeBackendKind.Legacy, selected);
        }

        [TestMethod]
        public void SelectUsesUnavailableBackendOnModernWindows11WhenUtilityIsMissing()
        {
            var selected = TaskbarThemeBackendSelector.Select(new Version(10, 0, 22621, 1343), false);

            Assert.AreEqual(TaskbarThemeBackendKind.Unavailable, selected);
        }

        [TestMethod]
        [DataRow("10.0.22621.1343")]
        [DataRow("10.0.22631.0")]
        [DataRow("10.0.26100.1")]
        public void SelectUsesModernBackendOnModernWindows11WhenUtilityIsAvailable(string version)
        {
            var selected = TaskbarThemeBackendSelector.Select(Version.Parse(version), true);

            Assert.AreEqual(TaskbarThemeBackendKind.Modern, selected);
        }
    }
}
