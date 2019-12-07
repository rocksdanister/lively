using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;

//credit: https://github.com/emoacht/WpfBuiltinDpiTest
namespace livelywpf
{
	internal class DpiHelper
	{
		#region Win32 (New)		

		[DllImport("User32.dll", SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		private static extern bool EnableNonClientDpiScaling(
			IntPtr hwnd);

		public enum DPI_AWARENESS
		{
			DPI_AWARENESS_INVALID = -1,
			DPI_AWARENESS_UNAWARE = 0,
			DPI_AWARENESS_SYSTEM_AWARE = 1,
			DPI_AWARENESS_PER_MONITOR_AWARE = 2
		}

		public enum DPI_AWARENESS_CONTEXT
		{
			DPI_AWARENESS_CONTEXT_DEFAULT = 0, // Undocumented
			DPI_AWARENESS_CONTEXT_UNAWARE = -1, // Undocumented
			DPI_AWARENESS_CONTEXT_SYSTEM_AWARE = -2, // Undocumented
			DPI_AWARENESS_CONTEXT_PER_MONITOR_AWARE = -3 // Undocumented
		}

		[DllImport("User32.dll")]
		public static extern DPI_AWARENESS_CONTEXT GetThreadDpiAwarenessContext();

		[DllImport("User32.dll")]
		public static extern DPI_AWARENESS_CONTEXT GetWindowDpiAwarenessContext(
			IntPtr hwnd);

		[DllImport("User32.dll")]
		public static extern DPI_AWARENESS_CONTEXT SetThreadDpiAwarenessContext(
			DPI_AWARENESS_CONTEXT dpiContext);

		[DllImport("User32.dll")]
		public static extern DPI_AWARENESS GetAwarenessFromDpiAwarenessContext(
			DPI_AWARENESS_CONTEXT value);

		[DllImport("User32.dll")]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool IsValidDpiAwarenessContext(
			DPI_AWARENESS_CONTEXT value);

		[DllImport("User32.dll")]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool AreDpiAwarenessContextsEqual(
			DPI_AWARENESS_CONTEXT dpiContextA,
			DPI_AWARENESS_CONTEXT dpiContextB);

		#endregion

		#region Win32 (Old)

		public enum PROCESS_DPI_AWARENESS
		{
			PROCESS_DPI_UNAWARE = 0,
			PROCESS_SYSTEM_DPI_AWARE = 1,
			PROCESS_PER_MONITOR_DPI_AWARE = 2
		}

		[DllImport("Shcore.dll", SetLastError = true)]
		public static extern int GetProcessDpiAwareness(
			IntPtr hprocess,
			out PROCESS_DPI_AWARENESS value);

		#endregion

		#region Win32 (Other)

		[DllImport("Kernel32.dll", SetLastError = true)]
		public static extern uint FormatMessage(
			uint dwFlags,
			IntPtr lpSource,
			uint dwMessageId,
			uint dwLanguageId,
			StringBuilder lpBuffer,
			int nSize,
			IntPtr Arguments);

		public const uint FORMAT_MESSAGE_FROM_SYSTEM = 0x00001000;

		#endregion

		public static bool EnableScaling(Window window)
		{
			var handle = new WindowInteropHelper(window).Handle;

			var result = EnableNonClientDpiScaling(handle);

			if (!result)
				CheckLastError();

			return result;
		}

		public static bool EnableScaling(Visual visual)
		{
			var source = PresentationSource.FromVisual(visual) as HwndSource;
			if (source == null)
				return false;

			var handle = source.Handle;

			var result = EnableNonClientDpiScaling(handle);

			if (!result)
				CheckLastError();

			return result;
		}

		public static DPI_AWARENESS SetThreadAwarenessContext(DPI_AWARENESS_CONTEXT newContext)
		{
			var oldContext = SetThreadDpiAwarenessContext(newContext);

			return GetAwarenessFromDpiAwarenessContext(oldContext);
		}

		public static DPI_AWARENESS GetThreadAwarenessContext()
		{
			var context = GetThreadDpiAwarenessContext();

			return GetAwarenessFromDpiAwarenessContext(context);
		}

		public static DPI_AWARENESS GetWindowAwarenessContext(Window window)
		{
			var handle = new WindowInteropHelper(window).Handle;

			var context = GetWindowDpiAwarenessContext(handle);

			return GetAwarenessFromDpiAwarenessContext(context);
		}

		public static PROCESS_DPI_AWARENESS GetProcessAwareness()
		{
			PROCESS_DPI_AWARENESS value;
			var result = GetProcessDpiAwareness(
				IntPtr.Zero, // Current process
				out value);

			if (result != 0) // 0 means S_OK.
				throw new InvalidOperationException();

			return value;
		}

		private static void CheckLastError()
		{
			var code = Marshal.GetLastWin32Error();

			var sb = new StringBuilder(512);

			FormatMessage(
			  FORMAT_MESSAGE_FROM_SYSTEM,
			  IntPtr.Zero,
			  (uint)code,
			  0x0409, // US (English)
			  sb,
			  sb.Capacity,
			  IntPtr.Zero);

			Debug.WriteLine($"Error Code: {code}, Message: {sb.ToString().Trim()}");
		}
	}
}