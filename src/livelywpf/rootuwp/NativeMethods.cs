using System.Runtime.InteropServices;

namespace rootuwp
{
    internal static class NativeMethods
    {
        [DllImport("user32.dll")]
        public static extern bool ShowWindow(System.IntPtr hWnd, int nCmdShow);
    }
}
