using Lively.Models.Enums;
using Lively.Services.Taskbar;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Drawing;

namespace Lively.Tests.Taskbar
{
    [TestClass]
    public sealed class TaskbarThemeColorTests
    {
        private static readonly Color AccentColor = Color.FromArgb(0x44, 0x11, 0x22, 0x33);

        [TestMethod]
        public void ToAbgrUsesAabbggrrPacking()
        {
            Assert.AreEqual(0x12332211u, TaskbarThemeColor.ToAbgr(0x12, AccentColor));
        }

        [TestMethod]
        public void ToUtilityColorSendsRgbWithoutAlpha()
        {
            Assert.AreEqual(0x00332211u, TaskbarThemeColor.ToUtilityColor(AccentColor));
        }

        [TestMethod]
        [DataRow(TaskbarTheme.clear, 0x00FFFFFFu)]
        [DataRow(TaskbarTheme.blur, 0x00000000u)]
        [DataRow(TaskbarTheme.fluent, 0x0A000000u)]
        [DataRow(TaskbarTheme.color, 0xC8332211u)]
        [DataRow(TaskbarTheme.wallpaper, 0xC8332211u)]
        [DataRow(TaskbarTheme.wallpaperFluent, 0x7D332211u)]
        public void ToLegacyGradientColorPreservesExistingAccentValues(TaskbarTheme theme, uint expected)
        {
            var state = new TaskbarThemeState(theme, AccentColor);

            Assert.AreEqual(expected, TaskbarThemeColor.ToLegacyGradientColor(state));
        }
    }
}
