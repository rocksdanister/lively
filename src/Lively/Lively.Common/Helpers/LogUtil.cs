using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace Lively.Common.Helpers
{
    public class LogUtil
    {
        /// <summary>
        /// Returns data stored in class object file.
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public static string PropertyList(object obj)
        {
            try
            {
                var props = obj.GetType().GetProperties();
                var sb = new StringBuilder();
                foreach (var p in props)
                {
                    sb.AppendLine(p.Name + ": " + p.GetValue(obj, null));
                }
                return sb.ToString();
            }
            catch
            {
                return "Failed to retrive properties of config file.";
            }
        }

        /// <summary>
        /// Write to log win32 error if GetLastWin32Error returns true.
        /// </summary>
        public static string GetWin32Error(string message,
            [CallerMemberName] string memberName = "",
            [CallerFilePath] string fileName = "",
            [CallerLineNumber] int lineNumber = 0)
        {
            int err = Marshal.GetLastWin32Error();
            return $"HRESULT: {err}, {message} at\n{fileName} ({lineNumber})\n{memberName}";
        }
    }
}
